// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Response model for thread join operations
/// Provides comprehensive information about the join result and thread status
/// 
/// This model includes all necessary information for:
/// - Frontend confirmation of successful join
/// - Error handling for join failures
/// - Thread state information for UI updates
/// - Analytics and monitoring data
/// </summary>
public record JoinThreadResponse
{
    /// <summary>
    /// Indicates if the join operation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Thread identifier that was joined
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// User identifier that joined the thread
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the user in the thread
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Role assigned to the user in the thread
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the user joined the thread
    /// </summary>
    public DateTimeOffset JoinedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current participant count in the thread
    /// </summary>
    public int ParticipantCount { get; init; }

    /// <summary>
    /// Thread topic or title
    /// </summary>
    public string? ThreadTopic { get; init; }

    /// <summary>
    /// The share history time used for this participant
    /// Indicates from when the user can see chat history
    /// </summary>
    public DateTimeOffset? ShareHistoryTime { get; init; }

    /// <summary>
    /// Error message if join operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Azure error code if applicable
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Additional metadata about the join operation
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Thread permissions for the joined user
    /// </summary>
    public string[]? Permissions { get; init; }
}
