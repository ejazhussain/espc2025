// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Result of attempting to claim a work item
/// Used to communicate success or conflict (race condition) to the caller
/// </summary>
public record ClaimWorkItemResult
{
    /// <summary>
    /// Whether the claim operation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The claimed work item (if successful)
    /// </summary>
    public AgentWorkItem? WorkItem { get; init; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Name of the agent who claimed the work item (if already claimed by another agent)
    /// </summary>
    public string? ClaimedBy { get; init; }

    /// <summary>
    /// When the work item was claimed (if already claimed by another agent)
    /// </summary>
    public DateTimeOffset? ClaimedAt { get; init; }
}

/// <summary>
/// Request model for claiming a work item
/// </summary>
public record ClaimWorkItemRequest
{
    /// <summary>
    /// ACS User ID of the agent claiming the work item
    /// </summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the agent claiming the work item
    /// </summary>
    public string AgentName { get; init; } = string.Empty;
}
