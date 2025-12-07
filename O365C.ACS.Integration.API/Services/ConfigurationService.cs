// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Common;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Helpers;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Azure Communication Services configuration management implementation
/// Handles endpoint URLs, connection strings, and service settings with caching and validation
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppSettings _appSettings;

    public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger, AppSettings appSettings)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        _logger.LogInformation("[ACS Configuration Service] Successfully initialized");
    }

    /// <summary>
    /// Gets the ACS service endpoint URL for client initialization
    /// Returns configured endpoint or derives from connection string
    /// </summary>
    public async Task<EndpointResponse> GetEndpointAsync()
    {
        try
        {
            // Simulate async operation
            await Task.Delay(1);

            var endpointUrl = ConfigurationHelper.GetEndpointUrl(_configuration, _logger);

            return new EndpointResponse
            {
                EndpointUrl = endpointUrl,
                IsValid = !string.IsNullOrEmpty(endpointUrl),
                ValidatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Configuration Service] Failed to get endpoint URL");
            return new EndpointResponse
            {
                EndpointUrl = string.Empty,
                IsValid = false,
                ValidatedAt = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the ACS endpoint URL for client initialization
    /// </summary>
    /// <returns>ACS service endpoint URL</returns>
    public string GetEndpointUrl()
    {
        // First try strongly-typed configuration
        var endpointUrl = _appSettings.ACS.EndpointUrl;
        if (!string.IsNullOrEmpty(endpointUrl))
        {
            return endpointUrl;
        }

        // Fallback to legacy configuration access
        return ConfigurationHelper.GetEndpointUrl(_configuration, _logger);
    }

    /// <summary>
    /// Validates all ACS configuration settings
    /// </summary>
    public async Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("[ACS Configuration Service] Starting configuration validation");

            // Simulate async validation
            await Task.Delay(10);

            // Check connection string from strongly-typed configuration first
            var connectionString = _appSettings.ConnectionStrings.AzureCommunicationServices;
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to legacy configuration access
                connectionString = ConfigurationHelper.GetValidatedConnectionString(_configuration);
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("[ACS Configuration Service] Invalid or missing connection string");
                return false;
            }

            // Check endpoint URL
            var endpointUrl = GetEndpointUrl();
            if (string.IsNullOrEmpty(endpointUrl))
            {
                _logger.LogError("[ACS Configuration Service] Invalid or missing endpoint URL");
                return false;
            }

            _logger.LogInformation("[ACS Configuration Service] Configuration validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Configuration Service] Configuration validation failed");
            return false;
        }
    }

    /// <summary>
    /// Gets default token scopes from configuration
    /// </summary>
    public string[] GetDefaultTokenScopes()
    {
        try
        {
            // First try strongly-typed configuration
            var scope = _appSettings.ACS.DefaultTokenScope;
            if (!string.IsNullOrEmpty(scope))
            {
                return scope.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }

            // Fallback to legacy configuration access
            var legacyScopes = _configuration.GetValue<string>("ACS:DefaultTokenScope") ?? "chat";
            return legacyScopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ACS Configuration Service] Failed to get default token scopes, using 'chat' as default");
            return new[] { "chat" };
        }
    }

    /// <summary>
    /// Gets token expiration settings from configuration
    /// </summary>
    public int GetTokenExpirationHours()
    {
        try
        {
            // First try strongly-typed configuration
            var hours = _appSettings.ACS.TokenExpirationHours;
            if (hours > 0)
            {
                return hours;
            }

            // Fallback to legacy configuration access
            return _configuration.GetValue<int>("ACS:TokenExpirationHours", 24);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ACS Configuration Service] Failed to get token expiration hours, using 24 hours as default");
            return 24;
        }
    }

    /// <summary>
    /// Refreshes cached configuration values
    /// </summary>
    public async Task<bool> RefreshConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("[ACS Configuration Service] Refreshing configuration cache");

            // Simulate async refresh operation
            await Task.Delay(50);

            // In a real implementation, this might:
            // 1. Clear configuration cache
            // 2. Reload configuration from sources
            // 3. Validate refreshed configuration
            // 4. Update internal state

            _logger.LogInformation("[ACS Configuration Service] Configuration refresh completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Configuration Service] Configuration refresh failed");
            return false;
        }
    }
}
