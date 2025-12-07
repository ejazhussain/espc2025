// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Common;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for Azure Communication Services configuration management
/// Handles endpoint URLs, connection strings, and service settings
/// 
/// Follows Azure best practices for:
/// - Secure configuration management
/// - Environment-specific settings
/// - Configuration validation and caching
/// - Service discovery and endpoint management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the ACS service endpoint URL for client initialization
    /// </summary>
    /// <returns>Endpoint response with ACS service URL</returns>
    Task<EndpointResponse> GetEndpointAsync();

    /// <summary>
    /// Gets the ACS endpoint URL for client initialization
    /// </summary>
    /// <returns>ACS service endpoint URL</returns>
    string GetEndpointUrl();

    /// <summary>
    /// Validates all ACS configuration settings
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    Task<bool> ValidateConfigurationAsync();

    /// <summary>
    /// Gets default token scopes from configuration
    /// </summary>
    /// <returns>Array of default communication token scopes</returns>
    string[] GetDefaultTokenScopes();

    /// <summary>
    /// Gets token expiration settings from configuration
    /// </summary>
    /// <returns>Token expiration in hours</returns>
    int GetTokenExpirationHours();

    /// <summary>
    /// Refreshes cached configuration values
    /// </summary>
    /// <returns>Success status of refresh operation</returns>
    Task<bool> RefreshConfigurationAsync();
}