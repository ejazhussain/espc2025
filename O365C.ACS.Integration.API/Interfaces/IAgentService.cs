// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Agent;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for support agent assignment and management
/// Simplified interface for agent routing and assignment
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Assigns an available support agent to a chat thread
    /// </summary>
    /// <param name="request">Agent assignment request with thread and requirements</param>
    /// <returns>Assignment response with agent details or failure reason</returns>
    Task<AssignAgentResponse> AssignAgentAsync(AssignAgentRequest request);
    
    
    /// <summary>
    /// Gets the Azure Communication Services user info for a given Teams user ID
    /// </summary>
    /// <param name="teamsUserId">The Teams user ID to get the ACS user info for</param>
    /// <returns>AgentUser containing the ACS user info for the given Teams user, or null if not found</returns>
    Task<AgentUser?> GetAgentAcsUserAsync(string teamsUserId);

    /// <summary>
    /// Creates a new agent work item in the database
    /// </summary>
    /// <param name="workItem">The agent work item to create</param>
    /// <returns>The created agent work item</returns>
    Task<AgentWorkItem> CreateAgentWorkItemAsync(AgentWorkItem workItem);

    /// <summary>
    /// Gets all agent work items from the database
    /// </summary>
    /// <param name="statusFilter">Optional status filter. If provided, only returns items with that status.</param>
    /// <returns>Collection of all agent work items (filtered by status if specified)</returns>
    Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsAsync(AgentWorkItemStatus? statusFilter = null);

    /// <summary>
    /// Updates an existing agent work item status
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to update</param>
    /// <param name="status">The new status to set</param>
    /// <returns>The updated agent work item</returns>
    Task<AgentWorkItem> UpdateAgentWorkItemAsync(string threadId, AgentWorkItemStatus status);

    /// <summary>
    /// Atomically claims a work item for an agent (prevents race conditions)
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to claim</param>
    /// <param name="agentId">The agent's ACS User ID</param>
    /// <param name="agentName">The agent's display name</param>
    /// <returns>Result indicating success or conflict</returns>
    Task<ClaimWorkItemResult> ClaimWorkItemAsync(string threadId, string agentId, string agentName);

    /// <summary>
    /// Gets all unassigned work items (in the queue)
    /// </summary>
    /// <returns>Collection of unassigned work items ordered by wait time</returns>
    Task<IEnumerable<AgentWorkItem>> GetUnassignedWorkItemsAsync();

    /// <summary>
    /// Gets all work items assigned to a specific agent
    /// </summary>
    /// <param name="agentId">The agent's ACS User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Collection of work items assigned to the agent</returns>
    Task<IEnumerable<AgentWorkItem>> GetWorkItemsByAgentIdAsync(string agentId, AgentWorkItemStatus? status = null);

    /// <summary>
    /// Deletes a work item (used when agent cancels or customer ends chat)
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteWorkItemAsync(string threadId);

    /// <summary>
    /// Cancels a work item by setting status to Cancelled (for tracking)
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to cancel</param>
    /// <returns>True if cancelled successfully, false if not found</returns>
    Task<bool> CancelWorkItemAsync(string threadId);
}