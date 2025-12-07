// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Token;

/// <summary>
/// ACS user information model
/// Contains essential user identity data for frontend consumption and SDK initialization
/// 
/// This model provides consistent user identity information across all operations
/// while ensuring compatibility with Azure Communication Services client SDKs.
/// </summary>
public record UserInfo
{
    /// <summary>
    /// ACS communication user identifier
    /// Used for client SDK initialization and operations
    /// Format: 8:acs:resource-id_user-id
    /// </summary>
    public string CommunicationUserId { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the user (optional)
    /// Used in chat threads and user interface
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// User creation timestamp
    /// Useful for auditing and user lifecycle management
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User status (Active, Inactive, Suspended)
    /// Used for access control and user management
    /// </summary>
    public string Status { get; init; } = "Active";
}