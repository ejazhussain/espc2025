// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Simplified response model for agent assignment operations
/// Returns agent user details and assignment status
/// Following Azure Communication Services best practices
/// </summary>
public class AssignAgentResponse
{
    /// <summary>
    /// Indicates whether the agent assignment was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The assigned agent's display name
    /// </summary>
    public string? AgentDisplayName { get; set; }

    /// <summary>
    /// The assigned agent's Azure Communication Services user ID
    /// </summary>
    public string? AgentUserId { get; set; }

    public string? TeamsUserId { get; set; }

    /// <summary>
    /// The thread ID where the agent was assigned
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Timestamp when the agent was assigned (UTC)
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Error message if assignment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic error handling
    /// </summary>
    public string? ErrorCode { get; set; }

}