// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Token;

/// <summary>
/// Response model for ACS user token operations
/// Maps to the frontend TypeScript interface expectations
/// 
/// This model ensures type safety and consistent API responses
/// across all ACS token operations while maintaining compatibility
/// with frontend TypeScript interfaces.
/// </summary>
public record UserTokenResponse
{
    /// <summary>
    /// JWT access token for Azure Communication Services
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Unique ACS user identifier
    /// Format: 8:acs:resource-id_user-id
    /// </summary>
    public string Identity { get; init; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp (UTC)
    /// </summary>
    public DateTimeOffset ExpiresOn { get; init; }

    /// <summary>
    /// Additional user information for frontend compatibility
    /// </summary>
    public UserInfo User { get; init; } = new();
}