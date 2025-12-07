// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;

namespace O365C.ACS.Integration.API.Helpers;

/// <summary>
/// Helper class for token-specific operations and utilities
/// Provides specialized functionality for ACS token management, validation, and lifecycle
/// 
/// Features:
/// - Token generation with enterprise security patterns
/// - Token validation and expiration management
/// - Scope management and compatibility checking
/// - Performance optimization for token operations
/// - Audit trail and compliance logging
/// 
/// Reference: https://docs.microsoft.com/en-us/azure/communication-services/concepts/identity-model
/// </summary>
public static class TokenOperationHelper
{
    /// <summary>
    /// Generates a unique operation identifier for token operations
    /// Useful for correlation, debugging, and audit trails
    /// </summary>
    /// <param name="operation">Token operation type (e.g., "CreateUser", "RefreshToken")</param>
    /// <param name="userId">Optional user identifier for context</param>
    /// <returns>Unique operation identifier with timestamp and context</returns>
    public static string GenerateTokenOperationId(string operation, string? userId = null)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var operationId = Guid.NewGuid().ToString("N")[..8]; // Short GUID for readability

        var context = string.IsNullOrEmpty(userId) ? "new" : userId.MaskForLogging();
        return $"token_{operation}_{context}_{timestamp}_{operationId}";
    }

    /// <summary>
    /// Creates a structured log context for token operations
    /// Provides consistent logging across all token-related operations
    /// </summary>
    /// <param name="operation">Operation name (e.g., "CreateUserAndToken", "RefreshToken")</param>
    /// <param name="userId">Optional user ID (will be masked for security)</param>
    /// <param name="scopes">Token scopes being requested</param>
    /// <returns>Structured log context for consistent logging</returns>
    public static Dictionary<string, object> CreateTokenLogContext(string operation, string? userId = null, CommunicationTokenScope[]? scopes = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["OperationId"] = GenerateTokenOperationId(operation, userId),
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["Component"] = "TokenManager"
        };

        if (!string.IsNullOrEmpty(userId))
        {
            context["UserId"] = userId.MaskForLogging();
        }

        if (scopes != null && scopes.Length > 0)
        {
            context["RequestedScopes"] = scopes.ToScopeString();
            context["ScopeCount"] = scopes.Length;
        }

        return context;
    }

    /// <summary>
    /// Validates token expiration settings and calculates optimal expiration time
    /// Ensures tokens don't exceed security policies while meeting operational needs
    /// </summary>
    /// <param name="requestedHours">Requested token expiration in hours</param>
    /// <param name="maxAllowedHours">Maximum allowed expiration (security policy)</param>
    /// <param name="defaultHours">Default expiration when not specified</param>
    /// <returns>Validated expiration time in hours</returns>
    public static int ValidateTokenExpiration(int? requestedHours, int maxAllowedHours = 720, int defaultHours = 24)
    {
        if (requestedHours == null)
        {
            return defaultHours;
        }

        if (requestedHours <= 0)
        {
            throw new ArgumentException("Token expiration must be greater than 0 hours", nameof(requestedHours));
        }

        if (requestedHours > maxAllowedHours)
        {
            throw new ArgumentException($"Token expiration cannot exceed {maxAllowedHours} hours (security policy)", nameof(requestedHours));
        }

        return requestedHours.Value;
    }

    /// <summary>
    /// Analyzes token scope requirements and provides optimization recommendations
    /// Helps with performance optimization and security best practices
    /// </summary>
    /// <param name="scopes">Requested communication token scopes</param>
    /// <returns>Scope analysis with recommendations and warnings</returns>
    public static TokenScopeAnalysis AnalyzeTokenScopes(CommunicationTokenScope[] scopes)
    {
        var analysis = new TokenScopeAnalysis
        {
            RequestedScopes = scopes,
            ScopeCount = scopes.Length,
            HasChatScope = scopes.Contains(CommunicationTokenScope.Chat),
            HasVoipScope = scopes.Contains(CommunicationTokenScope.VoIP),
            IsOptimal = true, // Default assumption
            Recommendations = new List<string>(),
            Warnings = new List<string>()
        };

        // Analyze scope combinations for optimization
        if (analysis.ScopeCount == 0)
        {
            analysis.IsOptimal = false;
            analysis.Warnings.Add("No scopes specified - will use default 'chat' scope");
        }

        if (analysis.ScopeCount > 3)
        {
            analysis.IsOptimal = false;
            analysis.Recommendations.Add("Consider reducing scope count for better performance");
        }

        if (analysis.HasVoipScope && !analysis.HasChatScope)
        {
            analysis.Recommendations.Add("Consider adding 'chat' scope for full communication capabilities");
        }

        // Security analysis
        var scopeString = scopes.ToScopeString();
        analysis.SecurityRating = CalculateSecurityRating(scopes);

        if (analysis.SecurityRating < 3)
        {
            analysis.Warnings.Add("High privilege scope combination - ensure proper authorization");
        }

        return analysis;
    }

    /// <summary>
    /// Calculates security rating for token scope combinations
    /// Higher numbers indicate lower security risk
    /// </summary>
    /// <param name="scopes">Token scopes to analyze</param>
    /// <returns>Security rating (1-5, where 5 is most secure)</returns>
    private static int CalculateSecurityRating(CommunicationTokenScope[] scopes)
    {
        // Base rating
        var rating = 5;

        // Reduce rating based on scope combinations
        if (scopes.Contains(CommunicationTokenScope.VoIP))
        {
            rating -= 1; // VoIP requires more careful handling
        }

        // Future scope types might affect rating differently
        if (scopes.Length > 2)
        {
            rating -= 1; // Multiple scopes increase attack surface
        }

        return Math.Max(1, rating); // Minimum rating of 1
    }

    /// <summary>
    /// Estimates token operation performance based on scope complexity
    /// Used for performance monitoring and optimization
    /// </summary>
    /// <param name="scopes">Token scopes being processed</param>
    /// <param name="isNewUser">Whether this is a new user creation</param>
    /// <returns>Estimated operation duration in milliseconds</returns>
    public static int EstimateOperationDuration(CommunicationTokenScope[] scopes, bool isNewUser)
    {
        var baseTime = isNewUser ? 500 : 200; // Base time in milliseconds
        var scopeTime = scopes.Length * 50; // Additional time per scope

        // VoIP operations might take longer
        if (scopes.Contains(CommunicationTokenScope.VoIP))
        {
            baseTime += 100;
        }

        return baseTime + scopeTime;
    }

    /// <summary>
    /// Creates audit log entry for token operations
    /// Ensures compliance with enterprise audit requirements
    /// </summary>
    /// <param name="operation">Token operation performed</param>
    /// <param name="userId">User identifier (will be masked)</param>
    /// <param name="scopes">Token scopes involved</param>
    /// <param name="success">Whether operation was successful</param>
    /// <param name="additionalContext">Additional audit context</param>
    /// <returns>Structured audit log entry</returns>
    public static Dictionary<string, object> CreateAuditLogEntry(
        string operation,
        string? userId,
        CommunicationTokenScope[]? scopes,
        bool success,
        Dictionary<string, object>? additionalContext = null)
    {
        var auditEntry = new Dictionary<string, object>
        {
            ["AuditType"] = "TokenOperation",
            ["Operation"] = operation,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["Success"] = success,
            ["Component"] = "ACS.TokenManager"
        };

        if (!string.IsNullOrEmpty(userId))
        {
            auditEntry["UserId"] = userId.MaskForLogging();
        }

        if (scopes != null)
        {
            auditEntry["Scopes"] = scopes.ToScopeString();
            auditEntry["ScopeAnalysis"] = AnalyzeTokenScopes(scopes);
        }

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                auditEntry[$"Context.{kvp.Key}"] = kvp.Value;
            }
        }

        return auditEntry;
    }
}

/// <summary>
/// Result of token scope analysis with recommendations and warnings
/// </summary>
public record TokenScopeAnalysis
{
    public CommunicationTokenScope[] RequestedScopes { get; set; } = Array.Empty<CommunicationTokenScope>();
    public int ScopeCount { get; set; }
    public bool HasChatScope { get; set; }
    public bool HasVoipScope { get; set; }
    public bool IsOptimal { get; set; }
    public int SecurityRating { get; set; } = 5;
    public List<string> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}