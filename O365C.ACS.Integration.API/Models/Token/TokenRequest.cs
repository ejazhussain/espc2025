// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Token;

/// <summary>
/// Request model for token generation operations
/// Encapsulates all necessary information for creating or refreshing ACS tokens
/// 
/// This model provides a consistent contract for token operations while
/// supporting various token generation scenarios and scope requirements.
/// </summary>
public record TokenRequest
{
    /// <summary>
    /// Existing user identifier (optional for new user creation)
    /// If provided, generates token for existing user
    /// If null/empty, creates new user and token
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Display name for the user (required for new users)
    /// Used in chat operations and user interface
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Comma-separated list of token scopes
    /// Supported values: chat, voip, pstn
    /// Default: "chat" if not specified
    /// </summary>
    public string Scopes { get; init; } = "chat";

    /// <summary>
    /// Optional token expiration override (in hours)
    /// Uses system default if not specified
    /// </summary>
    public int? ExpirationHours { get; init; }

    /// <summary>
    /// Additional metadata for token context
    /// Can include department, role, or other business context
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Request timestamp for auditing and correlation
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}