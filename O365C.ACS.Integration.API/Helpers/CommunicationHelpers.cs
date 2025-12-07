// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;

namespace O365C.ACS.Integration.API.Helpers;

/// <summary>
/// Helper class for ACS configuration management
/// Provides centralized configuration validation and retrieval
/// 
/// Features:
/// - Secure configuration validation
/// - Environment-specific settings
/// - Default value handling
/// - Configuration caching for performance
/// </summary>
public static class ACSConfigurationHelper
{
    /// <summary>
    /// Configuration key constants for consistency
    /// </summary>
    public static class ConfigKeys
    {
        public const string ConnectionString = "ConnectionStrings.AzureCommunicationServices";
        public const string EndpointUrl = "ACS:EndpointUrl";
        public const string DefaultTokenScope = "ACS:DefaultTokenScope";
        public const string TokenExpirationHours = "ACS:TokenExpirationHours";
        public const string MaxConcurrentOperations = "ACS:MaxConcurrentOperations";
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
    /// Gets ACS endpoint URL from configuration with fallback logic
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Optional logger for warnings</param>
    /// <returns>ACS endpoint URL or empty string if not found</returns>
    public static string GetEndpointUrl(IConfiguration configuration, ILogger? logger = null)
    {
        // Try explicit endpoint URL configuration first
        var endpointUrl = configuration[ConfigKeys.EndpointUrl];
        if (!string.IsNullOrWhiteSpace(endpointUrl))
        {
            return endpointUrl.Trim();
        }

        // Fallback to extracting from connection string
        try
        {
            var connectionString = GetValidatedConnectionString(configuration);
            var extractedEndpoint = connectionString.ExtractEndpointFromConnectionString();

            if (!string.IsNullOrEmpty(extractedEndpoint))
            {
                logger?.LogInformation("[ACS Config] Using endpoint URL extracted from connection string");
                return extractedEndpoint;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[ACS Config] Failed to extract endpoint from connection string");
        }

        logger?.LogWarning("[ACS Config] ACS endpoint URL not configured in {EndpointKey}", ConfigKeys.EndpointUrl);
        return string.Empty;
    }

    /// <summary>
    /// Gets default token scope from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Default scope string (defaults to "chat")</returns>
    public static string GetDefaultTokenScope(IConfiguration configuration)
    {
        return configuration[ConfigKeys.DefaultTokenScope] ?? "chat";
    }

    /// <summary>
    /// Gets token expiration hours from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Token expiration in hours (defaults to 24)</returns>
    public static int GetTokenExpirationHours(IConfiguration configuration)
    {
        var configValue = configuration[ConfigKeys.TokenExpirationHours];
        return int.TryParse(configValue, out var hours) && hours > 0 ? hours : 24;
    }

    /// <summary>
    /// Gets maximum concurrent operations limit from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Max concurrent operations (defaults to 10)</returns>
    public static int GetMaxConcurrentOperations(IConfiguration configuration)
    {
        var configValue = configuration[ConfigKeys.MaxConcurrentOperations];
        return int.TryParse(configValue, out var max) && max > 0 ? max : 10;
    }

    /// <summary>
    /// Validates all required ACS configuration settings
    /// Useful for startup validation to fail fast on misconfiguration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for validation messages</param>
    /// <returns>True if all required settings are valid</returns>
    public static bool ValidateConfiguration(IConfiguration configuration, ILogger logger)
    {
        try
        {
            logger.LogInformation("[ACS Config] Validating Azure Communication Services configuration...");

            // Validate connection string
            var connectionString = GetValidatedConnectionString(configuration);
            logger.LogInformation("[ACS Config] ✓ Connection string is valid");

            // Validate endpoint URL
            var endpointUrl = GetEndpointUrl(configuration, logger);
            if (string.IsNullOrEmpty(endpointUrl))
            {
                logger.LogWarning("[ACS Config] ⚠ Endpoint URL not configured - will extract from connection string");
            }
            else
            {
                logger.LogInformation("[ACS Config] ✓ Endpoint URL configured: {Endpoint}", endpointUrl);
            }

            // Log other configuration values
            var defaultScope = GetDefaultTokenScope(configuration);
            var tokenExpiration = GetTokenExpirationHours(configuration);
            var maxConcurrent = GetMaxConcurrentOperations(configuration);

            logger.LogInformation("[ACS Config] ✓ Default token scope: {Scope}", defaultScope);
            logger.LogInformation("[ACS Config] ✓ Token expiration: {Hours} hours", tokenExpiration);
            logger.LogInformation("[ACS Config] ✓ Max concurrent operations: {Max}", maxConcurrent);

            logger.LogInformation("[ACS Config] Configuration validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ACS Config] Configuration validation failed: {Error}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Helper class for common ACS operations and utilities
/// Provides reusable logic for ACS-specific tasks
/// </summary>
public static class ACSOperationHelper
{
    /// <summary>
    /// Generates a unique identifier for chat operations
    /// Useful for correlation and debugging
    /// </summary>
    /// <param name="prefix">Optional prefix for the identifier</param>
    /// <returns>Unique operation identifier</returns>
    public static string GenerateOperationId(string prefix = "acs")
    {
        return $"{prefix}_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}";
    }

    /// <summary>
    /// Creates a structured log context for ACS operations
    /// Provides consistent logging across all ACS operations
    /// </summary>
    /// <param name="operation">Operation name</param>
    /// <param name="userId">Optional user ID (will be masked)</param>
    /// <param name="threadId">Optional thread ID (will be masked)</param>
    /// <returns>Structured log context</returns>
    public static Dictionary<string, object> CreateLogContext(string operation, string? userId = null, string? threadId = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["OperationId"] = GenerateOperationId(),
            ["Timestamp"] = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrEmpty(userId))
        {
            context["UserId"] = userId.MaskForLogging();
        }

        if (!string.IsNullOrEmpty(threadId))
        {
            context["ThreadId"] = threadId.MaskForLogging();
        }

        return context;
    }

    /// <summary>
    /// Calculates retry delay using exponential backoff
    /// Used for resilient ACS operations
    /// </summary>
    /// <param name="attempt">Current attempt number (0-based)</param>
    /// <param name="baseDelayMs">Base delay in milliseconds</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds</param>
    /// <returns>Calculated delay in milliseconds</returns>
    public static int CalculateRetryDelay(int attempt, int baseDelayMs = 1000, int maxDelayMs = 30000)
    {
        var delay = baseDelayMs * Math.Pow(2, attempt);
        return Math.Min((int)delay, maxDelayMs);
    }
}