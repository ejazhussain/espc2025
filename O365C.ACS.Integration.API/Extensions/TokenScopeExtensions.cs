// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;

namespace O365C.ACS.Integration.API.Extensions;

/// <summary>
/// Extension methods for Azure Communication Services token scope operations
/// Provides helper methods for parsing, validating, and converting token scopes
/// 
/// These extensions follow Azure best practices for:
/// - Safe string-to-enum conversion with validation
/// - Case-insensitive scope parsing
/// - Default scope handling for missing values
/// - Error handling for invalid scope combinations
/// </summary>
public static class TokenScopeExtensions
{
    /// <summary>
    /// Supported ACS token scopes with their string representations
    /// </summary>
    private static readonly Dictionary<string, CommunicationTokenScope> ScopeMapping = new()
    {
        { "chat", CommunicationTokenScope.Chat },
        { "voip", CommunicationTokenScope.VoIP },
        // Add additional scopes as they become available in the SDK
        // { "pstn", CommunicationTokenScope.PSTN },
        // { "sms", CommunicationTokenScope.SMS }
    };

    /// <summary>
    /// Parses comma-separated token scopes from string
    /// Supports standard ACS scopes with case-insensitive matching
    /// </summary>
    /// <param name="scopeParam">Comma-separated scope string (e.g., "chat,voip")</param>
    /// <returns>Array of valid CommunicationTokenScope values</returns>
    public static CommunicationTokenScope[] ParseTokenScopes(this string scopeParam)
    {
        if (string.IsNullOrWhiteSpace(scopeParam))
        {
            return Array.Empty<CommunicationTokenScope>();
        }

        var scopeStrings = scopeParam.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var scopes = new List<CommunicationTokenScope>();

        foreach (var scopeString in scopeStrings)
        {
            var trimmedScope = scopeString.Trim().ToLowerInvariant();
            if (ScopeMapping.TryGetValue(trimmedScope, out var scope))
            {
                // Only add unique scopes
                if (!scopes.Contains(scope))
                {
                    scopes.Add(scope);
                }
            }
            // Invalid scopes are silently ignored - could add logging here if needed
        }

        return scopes.ToArray();
    }

    /// <summary>
    /// Validates that a collection of scopes contains at least one valid scope
    /// Throws appropriate exceptions for invalid input
    /// </summary>
    /// <param name="scopes">Collection of communication token scopes</param>
    /// <param name="parameterName">Parameter name for exception messages</param>
    /// <returns>Array of validated scopes</returns>
    /// <exception cref="ArgumentNullException">Thrown when scopes is null</exception>
    /// <exception cref="ArgumentException">Thrown when no valid scopes provided</exception>
    public static CommunicationTokenScope[] ValidateAndConvert(this IEnumerable<CommunicationTokenScope> scopes, string parameterName = "scopes")
    {
        var scopeArray = scopes?.ToArray() ?? throw new ArgumentNullException(parameterName);

        if (scopeArray.Length == 0)
        {
            throw new ArgumentException("At least one token scope is required", parameterName);
        }

        return scopeArray;
    }

    /// <summary>
    /// Gets default token scopes when none are specified
    /// Returns chat scope as the standard default for ACS operations
    /// </summary>
    /// <returns>Array containing default chat scope</returns>
    public static CommunicationTokenScope[] GetDefaultScopes()
    {
        return new[] { CommunicationTokenScope.Chat };
    }

    /// <summary>
    /// Validates that requested scopes are compatible with each other
    /// Some scope combinations may have restrictions in future SDK versions
    /// </summary>
    /// <param name="scopes">Array of token scopes to validate</param>
    /// <returns>True if scope combination is valid</returns>
    public static bool ValidateScopeCompatibility(this CommunicationTokenScope[] scopes)
    {
        // Currently all supported scopes are compatible
        // This method provides extension point for future scope validation rules
        return scopes.Length > 0;
    }

    /// <summary>
    /// Converts token scopes back to string representation
    /// Useful for logging and debugging
    /// </summary>
    /// <param name="scopes">Array of communication token scopes</param>
    /// <returns>Comma-separated string of scope names</returns>
    public static string ToScopeString(this CommunicationTokenScope[] scopes)
    {
        if (scopes == null || scopes.Length == 0)
        {
            return string.Empty;
        }

        var scopeNames = scopes.Select(scope =>
            ScopeMapping.FirstOrDefault(kvp => kvp.Value == scope).Key ?? scope.ToString().ToLowerInvariant()
        );

        return string.Join(",", scopeNames);
    }

    /// <summary>
    /// Gets human-readable description of token scopes
    /// Useful for user interfaces and documentation
    /// </summary>
    /// <param name="scopes">Array of communication token scopes</param>
    /// <returns>Human-readable description of capabilities</returns>
    public static string GetScopeDescription(this CommunicationTokenScope[] scopes)
    {
        if (scopes == null || scopes.Length == 0)
        {
            return "No capabilities";
        }

        var descriptions = new List<string>();

        foreach (var scope in scopes)
        {
            string description;
            if (scope.Equals(CommunicationTokenScope.Chat))
            {
                description = "Text messaging and chat";
            }
            else if (scope.Equals(CommunicationTokenScope.VoIP))
            {
                description = "Voice over IP calling";
            }
            else
            {
                description = scope.ToString();
            }
            descriptions.Add(description);
        }

        return string.Join(", ", descriptions);
    }
}