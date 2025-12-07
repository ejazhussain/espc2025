// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Request model for joining users to existing chat threads
/// Encapsulates all necessary information for adding participants to threads
/// 
/// This model supports various thread joining scenarios including:
/// - Customer joining their support thread
/// - Agent joining assigned threads
/// - Supervisor joining for escalation
/// - Team members joining collaboration threads
/// </summary>
public record JoinThreadRequest
{
    /// <summary>
    /// Unique identifier of the thread to join
    /// Required for all join operations
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// ACS user identifier of the user joining the thread
    /// Must be a valid Communication Services user ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the user in the thread
    /// Shown to other participants in the chat
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Role of the user in the thread
    /// Values: "customer", "agent", "supervisor", "member"
    /// </summary>
    public string Role { get; init; } = "customer";

    /// <summary>
    /// Optional metadata for the join operation
    /// Can include context about why the user is joining
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
