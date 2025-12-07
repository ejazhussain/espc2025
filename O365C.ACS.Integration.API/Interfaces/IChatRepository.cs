// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Agent;
using O365C.ACS.Integration.API.Models.Admin;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for chat repository operations
/// Handles persistence of chat-related data including agent work items and admin users
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Creates a new agent work item in the database
    /// </summary>
    /// <param name="workItem">The agent work item to create</param>
    /// <returns>The created work item with any server-generated properties</returns>
    Task<AgentWorkItem> CreateAgentWorkItemAsync(AgentWorkItem workItem);

    /// <summary>
    /// Gets all agent work items from the database
    /// </summary>
    /// <returns>List of all agent work items</returns>
    Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsAsync();

    /// <summary>
    /// Gets agent work items by status
    /// </summary>
    /// <param name="status">The status to filter by</param>
    /// <returns>List of work items with the specified status</returns>
    Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsByStatusAsync(AgentWorkItemStatus status);

    /// <summary>
    /// Gets agent work items by department
    /// </summary>
    /// <param name="department">The department to filter by</param>
    /// <returns>List of work items for the specified department</returns>
    Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsByDepartmentAsync(string department);

    /// <summary>
    /// Gets a specific agent work item by ID
    /// </summary>
    /// <param name="threadId">The thread ID (work item ID)</param>
    /// <returns>The work item if found, null otherwise</returns>
    Task<AgentWorkItem?> GetAgentWorkItemAsync(string threadId);

    /// <summary>
    /// Updates an existing agent work item's status
    /// </summary>
    /// <param name="threadId">The thread ID (work item ID)</param>
    /// <param name="status">The new status</param>
    /// <returns>The updated work item</returns>
    Task<AgentWorkItem?> UpdateAgentWorkItemStatusAsync(string threadId, AgentWorkItemStatus status);

    /// <summary>
    /// Updates an existing agent work item
    /// </summary>
    /// <param name="workItem">The work item with updated properties</param>
    /// <returns>The updated work item</returns>
    Task<AgentWorkItem?> UpdateAgentWorkItemAsync(AgentWorkItem workItem);

    /// <summary>
    /// Assigns an agent to a work item
    /// </summary>
    /// <param name="threadId">The thread ID (work item ID)</param>
    /// <param name="agentId">The agent ID</param>
    /// <param name="agentName">The agent display name</param>
    /// <returns>The updated work item</returns>
    Task<AgentWorkItem?> AssignAgentToWorkItemAsync(string threadId, string agentId, string agentName);

    /// <summary>
    /// Deletes an agent work item
    /// </summary>
    /// <param name="threadId">The thread ID (work item ID)</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAgentWorkItemAsync(string threadId);

    /// <summary>
    /// Atomically claims a work item for an agent using optimistic concurrency
    /// Prevents race conditions when multiple agents try to claim the same work item
    /// </summary>
    /// <param name="threadId">The thread ID (work item ID)</param>
    /// <param name="agentId">The agent's ACS User ID</param>
    /// <param name="agentName">The agent's display name</param>
    /// <returns>Result indicating success or conflict</returns>
    Task<ClaimWorkItemResult> ClaimAgentWorkItemAsync(string threadId, string agentId, string agentName);

    /// <summary>
    /// Gets all unassigned work items (in the queue)
    /// </summary>
    /// <returns>List of work items with status Unassigned, ordered by wait time (oldest first)</returns>
    Task<IEnumerable<AgentWorkItem>> GetUnassignedWorkItemsAsync();

    /// <summary>
    /// Gets all work items assigned to a specific agent
    /// </summary>
    /// <param name="agentId">The agent's ACS User ID</param>
    /// <param name="status">Optional status filter (Claimed, Active, or Resolved)</param>
    /// <returns>List of work items assigned to the agent</returns>
    Task<IEnumerable<AgentWorkItem>> GetWorkItemsByAgentIdAsync(string agentId, AgentWorkItemStatus? status = null);

    /// <summary>
    /// Deletes a work item from the database
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteWorkItemAsync(string threadId);

    /// <summary>
    /// Initializes the database and containers if they don't exist
    /// </summary>
    /// <returns>True if initialization was successful</returns>
    Task<bool> InitializeAsync();

    #region Admin User Management

    /// <summary>
    /// Gets or creates the system admin user for ACS operations
    /// This admin user is reused across all chat operations to avoid permission issues
    /// </summary>
    /// <returns>The admin user if found, null if not found</returns>
    Task<AdminUser?> GetAdminUserAsync();

    /// <summary>
    /// Creates or updates the system admin user
    /// </summary>
    /// <param name="adminUser">The admin user to save</param>
    /// <returns>The saved admin user</returns>
    Task<AdminUser> SaveAdminUserAsync(AdminUser adminUser);

    /// <summary>
    /// Updates the last used timestamp for the admin user
    /// </summary>
    /// <param name="adminUserId">The admin user ID</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateAdminUserLastUsedAsync(string adminUserId);

    #endregion

    #region Thread Metadata for Meetings

    /// <summary>
    /// Updates metadata for a chat thread (e.g., meeting information)
    /// </summary>
    /// <param name="threadId">The thread ID</param>
    /// <param name="metadata">Key-value pairs of metadata</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateThreadMetadataAsync(string threadId, Dictionary<string, string> metadata);

    /// <summary>
    /// Gets metadata for a chat thread
    /// </summary>
    /// <param name="threadId">The thread ID</param>
    /// <returns>Dictionary of metadata key-value pairs, or null if not found</returns>
    Task<Dictionary<string, string>?> GetThreadMetadataAsync(string threadId);

    /// <summary>
    /// Gets participants of a chat thread
    /// </summary>
    /// <param name="threadId">The thread ID</param>
    /// <returns>List of participant identifiers</returns>
    Task<List<ParticipantInfo>?> GetThreadParticipantsAsync(string threadId);

    #endregion
}

/// <summary>
/// Information about a thread participant
/// </summary>
public class ParticipantInfo
{
    public string? Identifier { get; set; }
    public string? DisplayName { get; set; }
}
