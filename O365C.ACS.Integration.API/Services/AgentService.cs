// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Communication.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Agent;
using O365C.ACS.Integration.API.Models.Admin;
using O365C.ACS.Integration.API.Models.Chat;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Helpers;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Azure Communication Services agent management implementation
/// Handles agent assignment, routing, availability, and workload distribution
/// </summary>
public class AgentService : IAgentService
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly ILogger<AgentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChatRepository _chatRepository;
    private readonly AppSettings _appSettings;
    private readonly IChatService _chatService;


    public AgentService(
        IConfiguration configuration,
        ILogger<AgentService> logger,
        IChatRepository chatRepository,
        AppSettings appSettings,
        IChatService chatService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

        try
        {
            var connectionString = GetAcsConnectionString();
            _identityClient = new CommunicationIdentityClient(connectionString);

            _logger.LogInformation("[ACS Agent Service] Successfully initialized with connection string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to initialize Communication Identity Client");
            throw;
        }
    }

    /// <summary>
    /// Gets ACS connection string from strongly-typed configuration with fallback
    /// </summary>
    private string GetAcsConnectionString()
    {
        // First try strongly-typed configuration
        var connectionString = _appSettings.ConnectionStrings.AzureCommunicationServices;
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fallback to legacy configuration access
        return ConfigurationHelper.GetValidatedConnectionString(_configuration);
    }

    /// <summary>
    /// Gets ACS endpoint URL from strongly-typed configuration with fallback
    /// </summary>
    private string GetAcsEndpointUrl()
    {
        // First try strongly-typed configuration
        var endpointUrl = _appSettings.ACS.EndpointUrl;
        if (!string.IsNullOrEmpty(endpointUrl))
        {
            return endpointUrl;
        }

        // Fallback to legacy configuration access
        return ConfigurationHelper.GetEndpointUrl(_configuration, _logger);
    }

    public async Task<AssignAgentResponse> AssignAgentAsync(AssignAgentRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.ThreadId))
            throw new ArgumentException("Thread ID is required", nameof(request.ThreadId));

        try
        {
            _logger.LogInformation("[ACS Agent Service] Assigning agent to thread: {ThreadId}", request.ThreadId);

            // For now, assign Alan Partridge as the agent
            // TODO: In production, implement sophisticated agent selection logic
            var agentUser = GetAgentUser("2a5de346-1d63-4c7a-897f-b1f4b5316fe5");

            // Add the agent user to the chat thread using ChatService's existing functionality
            var joinRequest = new JoinThreadRequest
            {
                ThreadId = request.ThreadId,
                UserId = agentUser.AcsUserId,
                DisplayName = agentUser.DisplayName,
                Role = "agent"  // Agents should see full conversation history including AI escalation
            };

            var joinResponse = await _chatService.JoinThreadAsync(joinRequest);

            if (!joinResponse.Success)
            {
                _logger.LogWarning("[ACS Agent Service] Failed to add agent to thread {ThreadId}: {ErrorMessage}", 
                    request.ThreadId, joinResponse.ErrorMessage);
                
                return new AssignAgentResponse
                {
                    Success = false,
                    ErrorMessage = joinResponse.ErrorMessage ?? "Failed to add agent to thread",
                    ErrorCode = "JOIN_THREAD_FAILED"
                };
            }

            _logger.LogInformation("[ACS Agent Service] Successfully assigned agent {AgentName} to thread {ThreadId}",
                agentUser.DisplayName, request.ThreadId);

            return new AssignAgentResponse
            {
                Success = true,
                AgentDisplayName = agentUser.DisplayName,
                AgentUserId = agentUser.AcsUserId,
                ThreadId = request.ThreadId,
                TeamsUserId = agentUser.TeamsUserId,
                AssignedAt = DateTime.UtcNow,
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("[ACS Agent Service] Thread not found or not accessible: {ThreadId}", request.ThreadId);
            return new AssignAgentResponse
            {
                Success = false,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to assign agent to thread: {ThreadId}", request.ThreadId);
            return new AssignAgentResponse
            {
                Success = false,
                ErrorMessage = "Failed to assign agent",
                ErrorCode = "ASSIGNMENT_FAILED"
            };
        }
    } 


    /// <summary>
    /// Gets the Azure Communication Services user info for a given Teams user ID
    /// Purpose: Map Teams user identity to corresponding ACS user for chat operations
    /// </summary>
    /// <param name="teamsUserId">The Teams user ID to get the ACS user info for</param>
    /// <returns>AgentUser containing the ACS user info for the given Teams user, or null if not found</returns>
    /// <exception cref="ArgumentException">Thrown when teamsUserId is null or empty</exception>
    public async Task<AgentUser?> GetAgentAcsUserAsync(string teamsUserId)
    {
        if (string.IsNullOrWhiteSpace(teamsUserId))
        {
            throw new ArgumentException("TeamsUserId is required and cannot be empty", nameof(teamsUserId));
        }

        try
        {
            _logger.LogInformation("[ACS Agent Service] Getting ACS user info for Teams user: {TeamsUserId}", teamsUserId);

            // Get all available agent users from configuration
            var agentUsers = await GetAllAgentUsersAsync();

            if (agentUsers == null || !agentUsers.Any())
            {
                _logger.LogWarning("[ACS Agent Service] No agent users found in configuration");
                return null;
            }
            
            var agentUser = agentUsers.FirstOrDefault(user =>
                !string.IsNullOrEmpty(user.TeamsUserId) &&
                user.TeamsUserId.Equals(teamsUserId, StringComparison.OrdinalIgnoreCase));

            if (agentUser == null)
            {
                _logger.LogWarning("[ACS Agent Service] No linked ACS user found for TeamsUserId: {TeamsUserId}", teamsUserId);
                return null;
            }

            _logger.LogInformation("[ACS Agent Service] Found ACS user {AcsUserId} for Teams user {TeamsUserId}",
                agentUser.AcsUserId, teamsUserId);

            return agentUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to get ACS user info for Teams user: {TeamsUserId}", teamsUserId);
            throw;
        }
    }

    /// <summary>
    /// Selects an agent user for assignment to a chat thread
    /// For now, returns Alan Partridge. In production, implement sophisticated agent selection logic
    /// </summary>
    /// <returns>The selected agent user</returns>
    private AgentUser GetAgentUser(string teamUserId)
    {        

        // Try to find Alan Partridge in the configured users
        var alanPartridge = _appSettings.AgentUsers?.Users?.FirstOrDefault(u =>
            u.TeamsUserId?.Equals(teamUserId, StringComparison.OrdinalIgnoreCase) == true);

        if (alanPartridge != null)
        {
            return new AgentUser
            {
                AcsUserId = alanPartridge.AcsUserId ?? "8:acs:default-alan-id",
                DisplayName = alanPartridge.DisplayName ?? "Alan Partridge",
                TeamsUserId = alanPartridge.TeamsUserId ?? string.Empty
            };
        }

        // Fallback to hardcoded values if not found in configuration
        return new AgentUser
        {
            AcsUserId = "8:acs:default-alan-id",
            DisplayName = "Alan Partridge"
        };
    }

    /// <summary>
    /// Gets all configured agent users from the application configuration or environment variables
    /// Purpose: Retrieve the complete list of available agents for assignment and routing
    /// Matches the TypeScript getAgentUsers implementation for consistency
    /// </summary>
    /// <returns>Collection of AgentUser objects representing all configured agents</returns>
    /// <exception cref="InvalidOperationException">Thrown when no agent users are configured</exception>
    private async Task<IEnumerable<AgentUser>> GetAllAgentUsersAsync()
    {
        try
        {
            _logger.LogInformation("[ACS Agent Service] Retrieving all configured agent users");

            // Get agent users from strongly-typed configuration
            var configUsers = _appSettings.AgentUsers?.Users;
            var agentUsers = new List<AgentUser>();

            if (configUsers != null && configUsers.Any())
            {
                _logger.LogInformation("[ACS Agent Service] Loading agent users from strongly-typed configuration");
                
                foreach (var configUser in configUsers)
                {
                    if (!string.IsNullOrWhiteSpace(configUser.AcsUserId) && !string.IsNullOrWhiteSpace(configUser.DisplayName))
                    {
                        agentUsers.Add(new AgentUser
                        {
                            AcsUserId = configUser.AcsUserId,
                            DisplayName = configUser.DisplayName,
                            TeamsUserId = configUser.TeamsUserId ?? string.Empty
                        });
                    }
                }
            }

            if (!agentUsers.Any())
            {
                _logger.LogWarning("[ACS Agent Service] No agent users found");
                throw new InvalidOperationException("No Agent user list provided");
            }

            _logger.LogInformation("[ACS Agent Service] Retrieved {Count} agent users", agentUsers.Count);
            await Task.CompletedTask;
            return agentUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to retrieve agent users");
            throw;
        }
    }

    /// <summary>
    /// Creates a new agent work item in the database
    /// Matches the TypeScript createAgentWorkItem function implementation
    /// </summary>
    /// <param name="workItem">The agent work item to create</param>
    /// <returns>The created agent work item</returns>
    /// <exception cref="ArgumentNullException">Thrown when workItem is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when creation fails</exception>
    public async Task<AgentWorkItem> CreateAgentWorkItemAsync(AgentWorkItem workItem)
    {
        if (workItem == null)
            throw new ArgumentNullException(nameof(workItem));

        try
        {
            _logger.LogInformation("[ACS Agent Service] Creating agent work item: {WorkItemId}", workItem.Id);

            var result = await _chatRepository.CreateAgentWorkItemAsync(workItem);

            _logger.LogInformation("[ACS Agent Service] Successfully created agent work item: {WorkItemId}", workItem.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to create agent work item: {WorkItemId}", workItem.Id);
            throw new InvalidOperationException($"Cosmos DB Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all agent work items from the database
    /// Matches the TypeScript getAgentWorkItems function implementation
    /// </summary>
    /// <returns>Collection of all agent work items</returns>
    /// <exception cref="InvalidOperationException">Thrown when retrieval fails</exception>
    public async Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsAsync(AgentWorkItemStatus? statusFilter = null)
    {
        try
        {
            if (statusFilter.HasValue)
            {
                _logger.LogInformation("[ACS Agent Service] Retrieving agent work items with status: {Status}", statusFilter.Value);
            }
            else
            {
                _logger.LogInformation("[ACS Agent Service] Retrieving all agent work items");
            }

            var result = await _chatRepository.GetAgentWorkItemsAsync();

            // Apply status filter if provided
            if (statusFilter.HasValue)
            {
                result = result.Where(item => item.Status == statusFilter.Value);
            }

            _logger.LogInformation("[ACS Agent Service] Successfully retrieved {Count} agent work items", result.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to retrieve agent work items");
            throw new InvalidOperationException($"Cosmos DB Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates an existing agent work item status
    /// Matches the TypeScript updateAgentWorkItem function implementation
    /// </summary>
    /// <param name="threadId">The thread ID of the work item to update</param>
    /// <param name="status">The new status to set</param>
    /// <returns>The updated agent work item</returns>
    /// <exception cref="ArgumentException">Thrown when threadId is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when update fails</exception>
    public async Task<AgentWorkItem> UpdateAgentWorkItemAsync(string threadId, AgentWorkItemStatus status)
    {
        if (string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentException("Thread ID is required", nameof(threadId));

        try
        {
            _logger.LogInformation("[ACS Agent Service] Updating agent work item {ThreadId} to status {Status}", threadId, status);

            // First, get the existing work item
            var existingItems = await _chatRepository.GetAgentWorkItemsAsync();
            var existingItem = existingItems.FirstOrDefault(item => item.Id == threadId);

            if (existingItem == null)
            {
                throw new InvalidOperationException($"Agent work item with thread ID {threadId} not found");
            }

            // Create updated work item with new status
            var updatedItem = existingItem with { Status = status };

            // Update in database
            var result = await _chatRepository.UpdateAgentWorkItemAsync(updatedItem);

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to update agent work item with thread ID {threadId}");
            }

            _logger.LogInformation("[ACS Agent Service] Successfully updated agent work item {ThreadId} to status {Status}", threadId, status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to update agent work item {ThreadId}", threadId);
            throw new InvalidOperationException($"Cosmos DB Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Atomically claims a work item for an agent
    /// Uses optimistic concurrency in the repository layer to prevent race conditions
    /// </summary>
    public async Task<ClaimWorkItemResult> ClaimWorkItemAsync(string threadId, string agentId, string agentName)
    {
        _logger.LogInformation("[ACS Agent Service] Claiming work item {ThreadId} for agent {AgentName}", threadId, agentName);

        try
        {
            // First, atomically claim the work item in the database
            var result = await _chatRepository.ClaimAgentWorkItemAsync(threadId, agentId, agentName);

            if (result.Success)
            {
                _logger.LogInformation("[ACS Agent Service] Successfully claimed work item {ThreadId} in database for agent {AgentName}",
                    threadId, agentName);

                // Now add the agent to the ACS chat thread
                try
                {
                    _logger.LogInformation("[ACS Agent Service] Adding agent {AgentId} to ACS thread {ThreadId}", agentId, threadId);

                    // Use ChatService to add the agent as a participant
                    var joinRequest = new JoinThreadRequest
                    {
                        ThreadId = threadId,
                        UserId = agentId,
                        DisplayName = agentName
                    };

                    var joinResponse = await _chatService.JoinThreadAsync(joinRequest);

                    if (joinResponse.Success)
                    {
                        _logger.LogInformation("[ACS Agent Service] Successfully added agent {AgentName} to ACS thread {ThreadId}",
                            agentName, threadId);
                    }
                    else
                    {
                        _logger.LogWarning("[ACS Agent Service] Failed to add agent {AgentName} to thread {ThreadId}: {Error}",
                            agentName, threadId, joinResponse.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ACS Agent Service] Failed to add agent {AgentName} to ACS thread {ThreadId}. Work item is claimed but agent not in chat.",
                        agentName, threadId);
                    // Work item is claimed but agent couldn't be added to chat
                    // This is a partial failure - the agent owns the work item but can't chat yet
                    // They may need to retry or contact support
                }
            }
            else
            {
                _logger.LogWarning("[ACS Agent Service] Failed to claim work item {ThreadId}: {Error}",
                    threadId, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Error claiming work item {ThreadId}", threadId);
            throw new InvalidOperationException($"Failed to claim work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all unassigned work items from the queue
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetUnassignedWorkItemsAsync()
    {
        _logger.LogInformation("[ACS Agent Service] Retrieving unassigned work items");

        try
        {
            var workItems = await _chatRepository.GetUnassignedWorkItemsAsync();

            _logger.LogInformation("[ACS Agent Service] Retrieved {Count} unassigned work items", workItems.Count());
            return workItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to retrieve unassigned work items");
            throw new InvalidOperationException($"Failed to retrieve unassigned work items: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all work items assigned to a specific agent
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetWorkItemsByAgentIdAsync(string agentId, AgentWorkItemStatus? status = null)
    {
        _logger.LogInformation("[ACS Agent Service] Retrieving work items for agent {AgentId} with status: {Status}",
            agentId, status?.ToString() ?? "All");

        try
        {
            var workItems = await _chatRepository.GetWorkItemsByAgentIdAsync(agentId, status);

            _logger.LogInformation("[ACS Agent Service] Retrieved {Count} work items for agent {AgentId}",
                workItems.Count(), agentId);
            return workItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to retrieve work items for agent {AgentId}", agentId);
            throw new InvalidOperationException($"Failed to retrieve work items for agent: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a work item (used when agent cancels or customer ends chat)
    /// </summary>
    public async Task<bool> DeleteWorkItemAsync(string threadId)
    {
        _logger.LogInformation("[ACS Agent Service] Deleting work item for thread {ThreadId}", threadId);

        try
        {
            var result = await _chatRepository.DeleteWorkItemAsync(threadId);

            if (result)
            {
                _logger.LogInformation("[ACS Agent Service] Successfully deleted work item for thread {ThreadId}", threadId);
            }
            else
            {
                _logger.LogWarning("[ACS Agent Service] Work item not found for thread {ThreadId}", threadId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to delete work item for thread {ThreadId}", threadId);
            throw new InvalidOperationException($"Failed to delete work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Cancels a work item by setting status to Cancelled (for tracking and history)
    /// </summary>
    public async Task<bool> CancelWorkItemAsync(string threadId)
    {
        _logger.LogInformation("[ACS Agent Service] Cancelling work item for thread {ThreadId}", threadId);

        try
        {
            // Update status to Cancelled instead of deleting
            var workItem = await _chatRepository.GetAgentWorkItemAsync(threadId);
            
            if (workItem == null)
            {
                _logger.LogWarning("[ACS Agent Service] Work item not found for thread {ThreadId}", threadId);
                return false;
            }

            // Don't cancel if already Resolved or Cancelled
            if (workItem.Status == AgentWorkItemStatus.Resolved || workItem.Status == AgentWorkItemStatus.Cancelled)
            {
                _logger.LogWarning("[ACS Agent Service] Cannot cancel work item {ThreadId} - already in final status {Status}", 
                    threadId, workItem.Status);
                return false;
            }

            // Cancel from any other status (Unassigned, Claimed, Active)
            _logger.LogInformation("[ACS Agent Service] Cancelling work item {ThreadId} from status {OldStatus} to Cancelled", 
                threadId, workItem.Status);

            await _chatRepository.UpdateAgentWorkItemStatusAsync(threadId, AgentWorkItemStatus.Cancelled);

            _logger.LogInformation("[ACS Agent Service] Successfully cancelled work item for thread {ThreadId}", threadId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Agent Service] Failed to cancel work item for thread {ThreadId}", threadId);
            throw new InvalidOperationException($"Failed to cancel work item: {ex.Message}", ex);
        }
    }
}
