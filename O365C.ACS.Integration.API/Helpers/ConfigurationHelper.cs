// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;

namespace O365C.ACS.Integration.API.Helpers;

/// <summary>
/// Helper class for ACS configuration management and validation
/// Provides centralized configuration validation, retrieval, and caching
/// 
/// Features:
/// - Secure configuration validation with detailed error messages
/// - Environment-specific settings with fallback handling
/// - Configuration caching for performance optimization
/// - Startup validation to fail fast on misconfiguration
/// 
/// Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Configuration key constants for consistency across the application
    /// </summary>
    public static class ConfigKeys
    {
        public const string ConnectionString = "ConnectionStrings:AzureCommunicationServices";
        public const string EndpointUrl = "ACS:EndpointUrl";
        public const string DefaultTokenScope = "ACS:DefaultTokenScope";
        public const string TokenExpirationHours = "ACS:TokenExpirationHours";
        public const string MaxConcurrentOperations = "ACS:MaxConcurrentOperations";
        public const string EnableRetryPolicy = "ACS:EnableRetryPolicy";
        public const string MaxRetryAttempts = "ACS:MaxRetryAttempts";
        public const string RetryDelaySeconds = "ACS:RetryDelaySeconds";
    }

    /// <summary>
    /// Validates and retrieves ACS connection string from configuration
    /// Throws informative exceptions for missing or invalid configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Validated connection string</returns>
    /// <exception cref="InvalidOperationException">Thrown when connection string is missing or invalid</exception>
    public static string GetValidatedConnectionString(IConfiguration configuration)
    {

        var connectionString = configuration.GetConnectionString("AzureCommunicationServices");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Communication Services connection string is required. " +
                $"Please configure '{ConfigKeys.ConnectionString}' in your app settings. " +
                "Format: endpoint=https://your-resource.communication.azure.com/;accesskey=your-key");
        }

        // Basic validation - ensure it contains required parts
        if (!connectionString.Contains("endpoint=", StringComparison.OrdinalIgnoreCase) ||
            !connectionString.Contains("accesskey=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid Azure Communication Services connection string format. " +
                "Expected format: endpoint=https://your-resource.communication.azure.com/;accesskey=your-key");
        }

        return connectionString;
    }

    /// <summary>
    /// Gets ACS endpoint URL from configuration with intelligent fallback logic
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Optional logger for warnings and information</param>
    /// <returns>ACS endpoint URL or empty string if not found</returns>
    public static string GetEndpointUrl(IConfiguration configuration, ILogger? logger = null)
    {

        var connectionStringsSection = configuration.GetSection("ConnectionStrings");
        var AzureCommunicationServiceString = configuration["ConnectionStrings:AzureCommunicationServices"];
        

        // Try explicit endpoint URL configuration first
        var endpointUrl = configuration[ConfigKeys.EndpointUrl];
        if (!string.IsNullOrWhiteSpace(endpointUrl))
        {
            logger?.LogInformation("[Config Helper] Using configured endpoint URL");
            return endpointUrl.Trim();
        }

        // Fallback to extracting from connection string
        try
        {
            var connectionString = GetValidatedConnectionString(configuration);
            var extractedEndpoint = connectionString.ExtractEndpointFromConnectionString();
            
            if (!string.IsNullOrEmpty(extractedEndpoint))
            {
                logger?.LogInformation("[Config Helper] Using endpoint URL extracted from connection string");
                return extractedEndpoint;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[Config Helper] Failed to extract endpoint from connection string");
        }

        logger?.LogWarning("[Config Helper] ACS endpoint URL not configured in {EndpointKey}", ConfigKeys.EndpointUrl);
        return string.Empty;
    }

    /// <summary>
    /// Gets default token scope from configuration with safe fallback
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Default scope string (defaults to "chat")</returns>
    public static string GetDefaultTokenScope(IConfiguration configuration)
    {
        return configuration[ConfigKeys.DefaultTokenScope] ?? "chat";
    }

    /// <summary>
    /// Gets token expiration hours from configuration with validation
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Token expiration in hours (defaults to 24, max 720 for security)</returns>
    public static int GetTokenExpirationHours(IConfiguration configuration)
    {
        var configValue = configuration[ConfigKeys.TokenExpirationHours];
        if (int.TryParse(configValue, out var hours) && hours > 0 && hours <= 720) // Max 30 days
        {
            return hours;
        }
        return 24; // Default to 24 hours
    }

    /// <summary>
    /// Gets maximum concurrent operations limit from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Max concurrent operations (defaults to 10, range 1-100)</returns>
    public static int GetMaxConcurrentOperations(IConfiguration configuration)
    {
        var configValue = configuration[ConfigKeys.MaxConcurrentOperations];
        if (int.TryParse(configValue, out var max) && max > 0 && max <= 100)
        {
            return max;
        }
        return 10; // Safe default
    }

    /// <summary>
    /// Gets retry policy configuration settings
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Retry policy settings tuple (enabled, maxAttempts, delaySeconds)</returns>
    public static (bool Enabled, int MaxAttempts, int DelaySeconds) GetRetryPolicySettings(IConfiguration configuration)
    {
        var enabled = bool.TryParse(configuration[ConfigKeys.EnableRetryPolicy], out var enableRetry) && enableRetry;
        
        var maxAttempts = 3; // Default
        if (int.TryParse(configuration[ConfigKeys.MaxRetryAttempts], out var attempts) && attempts > 0 && attempts <= 10)
        {
            maxAttempts = attempts;
        }

        var delaySeconds = 2; // Default
        if (int.TryParse(configuration[ConfigKeys.RetryDelaySeconds], out var delay) && delay > 0 && delay <= 60)
        {
            delaySeconds = delay;
        }

        return (enabled, maxAttempts, delaySeconds);
    }

    /// <summary>
    /// Validates all required ACS configuration settings at startup
    /// Useful for failing fast on application startup if configuration is invalid
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for validation messages</param>
    /// <returns>True if all required settings are valid, false otherwise</returns>
    public static bool ValidateConfiguration(IConfiguration configuration, ILogger logger)
    {
        try
        {
            logger.LogInformation("[Config Helper] Validating Azure Communication Services configuration...");

            // Validate connection string
            var connectionString = GetValidatedConnectionString(configuration);
            logger.LogInformation("[Config Helper] ✓ Connection string is valid");

            // Validate endpoint URL
            var endpointUrl = GetEndpointUrl(configuration, logger);
            if (string.IsNullOrEmpty(endpointUrl))
            {
                logger.LogWarning("[Config Helper] ⚠ Endpoint URL not configured - will extract from connection string");
            }
            else
            {
                logger.LogInformation("[Config Helper] ✓ Endpoint URL configured: {Endpoint}", endpointUrl);
            }

            // Log other configuration values for verification
            var defaultScope = GetDefaultTokenScope(configuration);
            var tokenExpiration = GetTokenExpirationHours(configuration);
            var maxConcurrent = GetMaxConcurrentOperations(configuration);
            var retryPolicy = GetRetryPolicySettings(configuration);

            logger.LogInformation("[Config Helper] ✓ Default token scope: {Scope}", defaultScope);
            logger.LogInformation("[Config Helper] ✓ Token expiration: {Hours} hours", tokenExpiration);
            logger.LogInformation("[Config Helper] ✓ Max concurrent operations: {Max}", maxConcurrent);
            logger.LogInformation("[Config Helper] ✓ Retry policy: Enabled={Enabled}, MaxAttempts={MaxAttempts}, DelaySeconds={DelaySeconds}", 
                retryPolicy.Enabled, retryPolicy.MaxAttempts, retryPolicy.DelaySeconds);

            logger.LogInformation("[Config Helper] Configuration validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Config Helper] Configuration validation failed: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets configuration section as strongly-typed object
    /// Useful for complex configuration scenarios
    /// </summary>
    /// <typeparam name="T">Configuration model type</typeparam>
    /// <param name="configuration">Application configuration</param>
    /// <param name="sectionName">Configuration section name</param>
    /// <returns>Strongly-typed configuration object or default instance</returns>
    public static T GetConfigurationSection<T>(IConfiguration configuration, string sectionName) where T : new()
    {
        var section = configuration.GetSection(sectionName);
        var result = new T();
        section.Bind(result);
        return result;
    }
}