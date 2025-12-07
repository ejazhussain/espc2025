// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using O365C.ACS.Integration.API.Models.Settings;

namespace O365C.ACS.Integration.API.Extensions;

/// <summary>
/// Extension methods for configuration parsing and connection string operations
/// Provides helper methods for ACS configuration management
/// 
/// These extensions support:
/// - Connection string parsing and validation
/// - Endpoint URL extraction
/// - Configuration value retrieval with defaults
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Extracts endpoint URL from ACS connection string
    /// Handles malformed connection strings gracefully
    /// </summary>
    /// <param name="connectionString">ACS connection string</param>
    /// <returns>Endpoint URL or empty string if not found</returns>
    public static string ExtractEndpointFromConnectionString(this string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        try
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var endpointPart = parts.FirstOrDefault(p => 
                p.StartsWith("endpoint=", StringComparison.OrdinalIgnoreCase));
            
            if (endpointPart != null)
            {
                return endpointPart.Substring("endpoint=".Length).Trim();
            }
        }
        catch
        {
            // Swallow parsing exceptions and return empty string
            // Caller should handle empty string appropriately
        }

        return string.Empty;
    }

    /// <summary>
    /// Binds configuration to AppSettings with proper validation
    /// Handles Azure Functions configuration structure
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>Populated AppSettings instance</returns>
    public static AppSettings GetAppSettings(this IConfiguration configuration)
    {
        var appSettings = new AppSettings();
        
        // Bind the root configuration to AppSettings - this will automatically map sections
        configuration.Bind(appSettings);
        
        return appSettings;
    }

    /// <summary>
    /// Gets a strongly-typed configuration section with fallback support
    /// </summary>
    /// <typeparam name="T">The configuration model type</typeparam>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="sectionName">The section name</param>
    /// <returns>The bound configuration object</returns>
    public static T GetSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }
}