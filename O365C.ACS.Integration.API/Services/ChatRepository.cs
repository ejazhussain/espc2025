// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Agent;
using O365C.ACS.Integration.API.Models.Admin;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Extensions;
using System.Net;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Cosmos DB implementation of chat repository
/// Handles persistence of agent work items and chat-related data
/// 
/// Features:
/// - Automatic database and container creation
/// - Partitioned storage for optimal performance
/// - Comprehensive error handling and logging
/// - Efficient querying with proper indexing
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<ChatRepository> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppSettings _appSettings;

    private const string DatabaseId = "messaging-teams-app-database";
    private const string ContainerName = "agentWorkItems";
    private const string AdminContainerName = "adminUsers";
    private const string PartitionKeyPath = "/partitionKey";
    private const string AdminPartitionKeyPath = "/environment";

    private Database? _database;
    private Container? _agentWorkItemsContainer;
    private Container? _adminUsersContainer;

    /// <summary>
    /// Initializes the ChatRepository with Cosmos DB client
    /// </summary>
    public ChatRepository(IConfiguration configuration, ILogger<ChatRepository> logger, AppSettings appSettings)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        try
        {
            // Get Cosmos DB configuration
            var connectionString = GetCosmosDbConnectionString();

            // Initialize Cosmos DB client
            _cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });

            _logger.LogInformation("[Chat Repository] Successfully initialized Cosmos DB client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to initialize Cosmos DB client");
            throw;
        }
    }

    /// <summary>
    /// Initializes the database and containers if they don't exist
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("[Chat Repository] Initializing Cosmos DB database and containers");

            // Create or get database
            _database = await CreateDatabaseAsync();

            // Create or get containers
            _agentWorkItemsContainer = await CreateContainerAsync(_database, ContainerName);
            _adminUsersContainer = await CreateAdminContainerAsync(_database, AdminContainerName);

            _logger.LogInformation("[Chat Repository] Successfully initialized Cosmos DB database and containers");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to initialize Cosmos DB database and containers");
            return false;
        }
    }

    /// <summary>
    /// Creates a new agent work item in the database
    /// </summary>
    public async Task<AgentWorkItem> CreateAgentWorkItemAsync(AgentWorkItem workItem)
    {
        workItem.Id.ValidateNotNullOrEmpty(nameof(workItem.Id));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Creating agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());

            // Note: UpdatedAt is init-only in the simplified model
            var response = await container.CreateItemAsync(workItem, new PartitionKey(workItem.PartitionKey));

            _logger.LogInformation("[Chat Repository] Successfully created agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("[Chat Repository] Agent work item {WorkItemId} already exists",
                workItem.Id.MaskForLogging());
            throw new InvalidOperationException($"Agent work item with ID {workItem.Id} already exists", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to create agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());
            throw new InvalidOperationException($"Failed to create agent work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all agent work items from the database
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsAsync()
    {
        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving all agent work items");

            var query = "SELECT * FROM c ORDER BY c.createdAt DESC";
            var iterator = container.GetItemQueryIterator<AgentWorkItem>(query);

            var results = new List<AgentWorkItem>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("[Chat Repository] Successfully retrieved {Count} agent work items", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve agent work items");
            throw new InvalidOperationException($"Failed to retrieve agent work items: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets agent work items by status
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsByStatusAsync(AgentWorkItemStatus status)
    {
        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving agent work items with status {Status}", status);

            var query = "SELECT * FROM c WHERE c.status = @status ORDER BY c.createdAt DESC";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@status", status.ToString());

            var iterator = container.GetItemQueryIterator<AgentWorkItem>(queryDefinition);

            var results = new List<AgentWorkItem>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("[Chat Repository] Successfully retrieved {Count} agent work items with status {Status}",
                results.Count, status);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve agent work items by status {Status}", status);
            throw new InvalidOperationException($"Failed to retrieve agent work items by status: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets agent work items by department
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetAgentWorkItemsByDepartmentAsync(string department)
    {
        department.ValidateNotNullOrEmpty(nameof(department));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving agent work items for department {Department}", department);

            var query = "SELECT * FROM c WHERE c.department = @department ORDER BY c.createdAt DESC";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@department", department);

            var iterator = container.GetItemQueryIterator<AgentWorkItem>(queryDefinition);

            var results = new List<AgentWorkItem>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("[Chat Repository] Successfully retrieved {Count} agent work items for department {Department}",
                results.Count, department);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve agent work items by department {Department}", department);
            throw new InvalidOperationException($"Failed to retrieve agent work items by department: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a specific agent work item by ID
    /// </summary>
    public async Task<AgentWorkItem?> GetAgentWorkItemAsync(string threadId)
    {
        threadId.ValidateNotNullOrEmpty(nameof(threadId));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving agent work item {ThreadId}",
                threadId.MaskForLogging());

            // We need to query since we don't know the partition key
            var query = "SELECT * FROM c WHERE c.id = @threadId";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@threadId", threadId);

            var iterator = container.GetItemQueryIterator<AgentWorkItem>(queryDefinition);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var workItem = response.FirstOrDefault();

                if (workItem != null)
                {
                    _logger.LogInformation("[Chat Repository] Successfully retrieved agent work item {ThreadId}",
                        threadId.MaskForLogging());
                    return workItem;
                }
            }

            _logger.LogInformation("[Chat Repository] Agent work item {ThreadId} not found",
                threadId.MaskForLogging());
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve agent work item {ThreadId}",
                threadId.MaskForLogging());
            throw new InvalidOperationException($"Failed to retrieve agent work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates an existing agent work item's status
    /// </summary>
    public async Task<AgentWorkItem?> UpdateAgentWorkItemStatusAsync(string threadId, AgentWorkItemStatus status)
    {
        try
        {
            var existingItem = await GetAgentWorkItemAsync(threadId);
            if (existingItem == null)
            {
                _logger.LogWarning("[Chat Repository] Cannot update status - agent work item {ThreadId} not found",
                    threadId.MaskForLogging());
                return null;
            }

            // Update status and timestamp - simplified for the new model
            var updatedItem = existingItem with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return await UpdateAgentWorkItemAsync(updatedItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to update agent work item status {ThreadId}",
                threadId.MaskForLogging());
            throw;
        }
    }

    /// <summary>
    /// Updates an existing agent work item
    /// </summary>
    public async Task<AgentWorkItem?> UpdateAgentWorkItemAsync(AgentWorkItem workItem)
    {
        workItem.Id.ValidateNotNullOrEmpty(nameof(workItem.Id));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Updating agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());

            // Ensure updated timestamp is current
            var updatedItem = workItem with { UpdatedAt = DateTimeOffset.UtcNow };

            var response = await container.ReplaceItemAsync(updatedItem, updatedItem.Id, new PartitionKey(updatedItem.PartitionKey));

            _logger.LogInformation("[Chat Repository] Successfully updated agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("[Chat Repository] Agent work item {WorkItemId} not found for update",
                workItem.Id.MaskForLogging());
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to update agent work item {WorkItemId}",
                workItem.Id.MaskForLogging());
            throw new InvalidOperationException($"Failed to update agent work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Assigns an agent to a work item
    /// </summary>
    public async Task<AgentWorkItem?> AssignAgentToWorkItemAsync(string threadId, string agentId, string agentName)
    {
        try
        {
            var existingItem = await GetAgentWorkItemAsync(threadId);
            if (existingItem == null)
            {
                _logger.LogWarning("[Chat Repository] Cannot assign agent - work item {ThreadId} not found",
                    threadId.MaskForLogging());
                return null;
            }

            // Update status to Active (simplified model doesn't track agent assignments)
            var updatedItem = existingItem with
            {
                Status = AgentWorkItemStatus.Active,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("[Chat Repository] Agent {AgentId} ({AgentName}) assigned to work item {ThreadId}",
                agentId, agentName, threadId.MaskForLogging());

            return await UpdateAgentWorkItemAsync(updatedItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to assign agent to work item {ThreadId}",
                threadId.MaskForLogging());
            throw;
        }
    }

    /// <summary>
    /// Atomically claims a work item for an agent using optimistic concurrency (ETag)
    /// This prevents race conditions when multiple agents try to claim the same work item
    /// </summary>
    public async Task<ClaimWorkItemResult> ClaimAgentWorkItemAsync(string threadId, string agentId, string agentName)
    {
        threadId.ValidateNotNullOrEmpty(nameof(threadId));
        agentId.ValidateNotNullOrEmpty(nameof(agentId));
        agentName.ValidateNotNullOrEmpty(nameof(agentName));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Agent {AgentName} attempting to claim work item {ThreadId}",
                agentName, threadId.MaskForLogging());

            // Step 1: Read the current work item WITH its ETag (version tag)
            ItemResponse<AgentWorkItem> response;
            try
            {
                response = await container.ReadItemAsync<AgentWorkItem>(
                    threadId,
                    new PartitionKey(threadId)
                );
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[Chat Repository] Work item {ThreadId} not found", threadId.MaskForLogging());
                return new ClaimWorkItemResult
                {
                    Success = false,
                    Error = "Work item not found"
                };
            }

            var workItem = response.Resource;
            string etag = response.ETag;

            // Step 2: Validate that the work item is still UNASSIGNED
            if (workItem.Status != AgentWorkItemStatus.Unassigned)
            {
                _logger.LogWarning("[Chat Repository] Work item {ThreadId} is not unassigned (status: {Status})",
                    threadId.MaskForLogging(), workItem.Status);

                return new ClaimWorkItemResult
                {
                    Success = false,
                    Error = workItem.AssignedAgentName != null
                        ? $"This chat has already been claimed by {workItem.AssignedAgentName}"
                        : "This chat is no longer available",
                    ClaimedBy = workItem.AssignedAgentName,
                    ClaimedAt = workItem.ClaimedAt
                };
            }

            // Step 3: Create updated work item with new ownership
            var now = DateTimeOffset.UtcNow;
            var updatedWorkItem = workItem with
            {
                Status = AgentWorkItemStatus.Claimed,
                AssignedAgentId = agentId,
                AssignedAgentName = agentName,
                ClaimedAt = now,
                UpdatedAt = now
            };

            // Step 4: Perform atomic update with ETag check (optimistic concurrency)
            // This will ONLY succeed if the ETag hasn't changed (no one else modified it)
            try
            {
                var updateResponse = await container.ReplaceItemAsync(
                    updatedWorkItem,
                    threadId,
                    new PartitionKey(threadId),
                    new ItemRequestOptions { IfMatchEtag = etag }  // CRITICAL: Atomic check
                );

                _logger.LogInformation("[Chat Repository] Successfully claimed work item {ThreadId} for agent {AgentName}",
                    threadId.MaskForLogging(), agentName);

                return new ClaimWorkItemResult
                {
                    Success = true,
                    WorkItem = updateResponse.Resource
                };
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                // ETag mismatch! Someone else modified the work item between our read and write
                _logger.LogWarning("[Chat Repository] Race condition detected - work item {ThreadId} was modified by another agent",
                    threadId.MaskForLogging());

                // Try to fetch the current state to see who claimed it
                try
                {
                    var currentState = await container.ReadItemAsync<AgentWorkItem>(threadId, new PartitionKey(threadId));
                    return new ClaimWorkItemResult
                    {
                        Success = false,
                        Error = $"This chat was just claimed by {currentState.Resource.AssignedAgentName ?? "another agent"}",
                        ClaimedBy = currentState.Resource.AssignedAgentName,
                        ClaimedAt = currentState.Resource.ClaimedAt
                    };
                }
                catch
                {
                    return new ClaimWorkItemResult
                    {
                        Success = false,
                        Error = "This chat was just claimed by another agent"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to claim work item {ThreadId}", threadId.MaskForLogging());
            throw new InvalidOperationException($"Failed to claim work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all unassigned work items (in the queue)
    /// Ordered by creation time (oldest first = longest wait time)
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetUnassignedWorkItemsAsync()
    {
        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving unassigned work items");

            // Query for UNASSIGNED status, ordered by oldest first (longest wait)
            var query = "SELECT * FROM c WHERE c.status = 0 ORDER BY c.createdAt ASC";
            var iterator = container.GetItemQueryIterator<AgentWorkItem>(query);

            var results = new List<AgentWorkItem>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("[Chat Repository] Retrieved {Count} unassigned work items", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve unassigned work items");
            throw new InvalidOperationException($"Failed to retrieve unassigned work items: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all work items assigned to a specific agent
    /// Ordered by most recent update first
    /// </summary>
    public async Task<IEnumerable<AgentWorkItem>> GetWorkItemsByAgentIdAsync(string agentId, AgentWorkItemStatus? status = null)
    {
        agentId.ValidateNotNullOrEmpty(nameof(agentId));

        try
        {
            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving work items for agent {AgentId} with status filter: {Status}",
                agentId, status?.ToString() ?? "All");

            QueryDefinition queryDefinition;
            if (status.HasValue)
            {
                // Filter by both agent ID and status
                var query = "SELECT * FROM c WHERE c.assignedAgentId = @agentId AND c.status = @status ORDER BY c.updatedAt DESC";
                queryDefinition = new QueryDefinition(query)
                    .WithParameter("@agentId", agentId)
                    .WithParameter("@status", (int)status.Value);
            }
            else
            {
                // Filter by agent ID only
                var query = "SELECT * FROM c WHERE c.assignedAgentId = @agentId ORDER BY c.updatedAt DESC";
                queryDefinition = new QueryDefinition(query)
                    .WithParameter("@agentId", agentId);
            }

            var iterator = container.GetItemQueryIterator<AgentWorkItem>(queryDefinition);

            var results = new List<AgentWorkItem>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("[Chat Repository] Retrieved {Count} work items for agent {AgentId}",
                results.Count, agentId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve work items for agent {AgentId}", agentId);
            throw new InvalidOperationException($"Failed to retrieve work items for agent: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes an agent work item
    /// </summary>
    public async Task<bool> DeleteAgentWorkItemAsync(string threadId)
    {
        threadId.ValidateNotNullOrEmpty(nameof(threadId));

        try
        {
            var existingItem = await GetAgentWorkItemAsync(threadId);
            if (existingItem == null)
            {
                _logger.LogWarning("[Chat Repository] Cannot delete - agent work item {ThreadId} not found",
                    threadId.MaskForLogging());
                return false;
            }

            var container = await GetAgentWorkItemsContainerAsync();

            _logger.LogInformation("[Chat Repository] Deleting agent work item {ThreadId}",
                threadId.MaskForLogging());

            await container.DeleteItemAsync<AgentWorkItem>(threadId, new PartitionKey(existingItem.PartitionKey));

            _logger.LogInformation("[Chat Repository] Successfully deleted agent work item {ThreadId}",
                threadId.MaskForLogging());

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("[Chat Repository] Agent work item {ThreadId} not found for deletion",
                threadId.MaskForLogging());
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to delete agent work item {ThreadId}",
                threadId.MaskForLogging());
            throw new InvalidOperationException($"Failed to delete agent work item: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a work item (alias for DeleteAgentWorkItemAsync to match interface)
    /// </summary>
    public async Task<bool> DeleteWorkItemAsync(string threadId)
    {
        return await DeleteAgentWorkItemAsync(threadId);
    }

    #region Admin User Management

    /// <summary>
    /// Gets the system admin user for ACS operations
    /// </summary>
    public async Task<AdminUser?> GetAdminUserAsync()
    {
        try
        {
            var container = await GetAdminUsersContainerAsync();

            _logger.LogInformation("[Chat Repository] Retrieving system admin user");

            // Query for any existing admin user (since we're reusing existing structure)
            var query = "SELECT * FROM c WHERE c.isActive = true ORDER BY c.lastUsedAt DESC";
            var queryDefinition = new QueryDefinition(query);

            var iterator = container.GetItemQueryIterator<AdminUser>(queryDefinition);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var adminUser = response.FirstOrDefault();

                if (adminUser != null)
                {
                    _logger.LogInformation("[Chat Repository] Successfully retrieved existing admin user {AdminUserId}", 
                        adminUser.Id);
                    return adminUser;
                }
            }

            _logger.LogInformation("[Chat Repository] No active admin user found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to retrieve system admin user");
            throw new InvalidOperationException($"Failed to retrieve system admin user: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates or updates the system admin user
    /// </summary>
    public async Task<AdminUser> SaveAdminUserAsync(AdminUser adminUser)
    {
        adminUser.Id.ValidateNotNullOrEmpty(nameof(adminUser.Id));

        try
        {
            var container = await GetAdminUsersContainerAsync();

            _logger.LogInformation("[Chat Repository] Saving admin user {AdminUserId}",
                adminUser.Id);

            // Update existing admin user with current timestamp
            var updatedAdminUser = adminUser with { LastUsedAt = DateTimeOffset.UtcNow };
            var response = await container.UpsertItemAsync(updatedAdminUser, new PartitionKey(updatedAdminUser.Environment));
            
            _logger.LogInformation("[Chat Repository] Successfully saved admin user {AdminUserId}", adminUser.Id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to save admin user {AdminUserId}", adminUser.Id);
            throw new InvalidOperationException($"Failed to save admin user: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates the last used timestamp for the admin user
    /// </summary>
    public async Task<bool> UpdateAdminUserLastUsedAsync(string adminUserId)
    {
        try
        {
            var existingAdminUser = await GetAdminUserAsync();
            if (existingAdminUser == null)
            {
                _logger.LogWarning("[Chat Repository] Cannot update last used - admin user not found");
                return false;
            }

            var updatedAdminUser = existingAdminUser with { LastUsedAt = DateTimeOffset.UtcNow };
            await SaveAdminUserAsync(updatedAdminUser);

            _logger.LogInformation("[Chat Repository] Updated admin user last used timestamp");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to update admin user last used timestamp");
            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets Cosmos DB connection string from configuration
    /// </summary>
    private string GetCosmosDbConnectionString()
    {
        // First try connection string from strongly-typed configuration
        var connectionString = _appSettings.ConnectionStrings.CosmosDb;
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fallback to individual settings from strongly-typed configuration
        var endpoint = _appSettings.CosmosDb.Endpoint;
        var key = _appSettings.CosmosDb.Key;

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            return $"AccountEndpoint={endpoint};AccountKey={key}";
        }

        // Final fallback to legacy configuration access
        connectionString = _configuration.GetConnectionString("CosmosDb");
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Legacy fallback to individual settings
        endpoint = _configuration["CosmosDb:Endpoint"];
        key = _configuration["CosmosDb:Key"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            return $"AccountEndpoint={endpoint};AccountKey={key}";
        }

        throw new InvalidOperationException("Cosmos DB connection string or endpoint/key not configured");
    }

    /// <summary>
    /// Creates or gets the database
    /// </summary>
    private async Task<Database> CreateDatabaseAsync()
    {
        if (_database != null)
        {
            return _database;
        }

        _logger.LogInformation("[Chat Repository] Creating or getting database {DatabaseId}", DatabaseId);

        var response = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
        _database = response.Database;

        _logger.LogInformation("[Chat Repository] Database {DatabaseId} is ready", DatabaseId);
        return _database;
    }

    /// <summary>
    /// Creates or gets a container
    /// </summary>
    private async Task<Container> CreateContainerAsync(Database database, string containerId)
    {
        _logger.LogInformation("[Chat Repository] Creating or getting container {ContainerId}", containerId);

        var containerProperties = new ContainerProperties(containerId, PartitionKeyPath)
        {
            // Add indexing policy for efficient queries
            IndexingPolicy = new IndexingPolicy
            {
                IndexingMode = IndexingMode.Consistent,
                Automatic = true
            }
        };

        // Add specific paths for better query performance
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/status/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/assignedAgentId/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/department/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/createdAt/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/updatedAt/?" });

        var response = await database.CreateContainerIfNotExistsAsync(containerProperties);

        _logger.LogInformation("[Chat Repository] Container {ContainerId} is ready", containerId);
        return response.Container;
    }

    /// <summary>
    /// Creates or gets the admin users container with the correct partition key
    /// </summary>
    private async Task<Container> CreateAdminContainerAsync(Database database, string containerId)
    {
        _logger.LogInformation("[Chat Repository] Creating or getting admin container {ContainerId}", containerId);

        var containerProperties = new ContainerProperties(containerId, AdminPartitionKeyPath)
        {
            // Add indexing policy for efficient queries
            IndexingPolicy = new IndexingPolicy
            {
                IndexingMode = IndexingMode.Consistent,
                Automatic = true
            }
        };

        // Add specific paths for better query performance on admin users
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/acsUserId/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/isActive/?" });
        containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/lastUsedAt/?" });

        var response = await database.CreateContainerIfNotExistsAsync(containerProperties);

        _logger.LogInformation("[Chat Repository] Admin container {ContainerId} is ready", containerId);
        return response.Container;
    }

    /// <summary>
    /// Gets the agent work items container
    /// </summary>
    private async Task<Container> GetAgentWorkItemsContainerAsync()
    {
        if (_agentWorkItemsContainer != null)
        {
            return _agentWorkItemsContainer;
        }

        if (_database == null)
        {
            _database = await CreateDatabaseAsync();
        }

        _agentWorkItemsContainer = await CreateContainerAsync(_database, ContainerName);
        return _agentWorkItemsContainer;
    }

    /// <summary>
    /// Gets the admin users container
    /// </summary>
    private async Task<Container> GetAdminUsersContainerAsync()
    {
        if (_adminUsersContainer != null)
        {
            return _adminUsersContainer;
        }

        if (_database == null)
        {
            _database = await CreateDatabaseAsync();
        }

        _adminUsersContainer = await CreateAdminContainerAsync(_database, AdminContainerName);
        return _adminUsersContainer;
    }

    #endregion

    #region Thread Metadata for Meetings

    /// <summary>
    /// Updates metadata for a chat thread (stores meeting information)
    /// </summary>
    public async Task<bool> UpdateThreadMetadataAsync(string threadId, Dictionary<string, string> metadata)
    {
        try
        {
            var container = await GetAgentWorkItemsContainerAsync();
            var workItem = await GetAgentWorkItemAsync(threadId);

            if (workItem == null)
            {
                _logger.LogWarning("[Chat Repository] Work item not found for thread {ThreadId}", threadId.MaskForLogging());
                return false;
            }

            // Update or add metadata (using record with expression since AgentWorkItem is a record)
            var existingMetadata = workItem.Metadata ?? new Dictionary<string, string>();
            var updatedMetadata = new Dictionary<string, string>(existingMetadata);
            
            foreach (var kvp in metadata)
            {
                updatedMetadata[kvp.Key] = kvp.Value;
            }

            var updatedWorkItem = workItem with 
            { 
                Metadata = updatedMetadata,
                LastModifiedAt = DateTime.UtcNow
            };

            await container.ReplaceItemAsync(
                updatedWorkItem,
                updatedWorkItem.Id,
                new PartitionKey(updatedWorkItem.PartitionKey));

            _logger.LogInformation("[Chat Repository] Updated metadata for thread {ThreadId}", threadId.MaskForLogging());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to update thread metadata");
            return false;
        }
    }

    /// <summary>
    /// Gets metadata for a chat thread
    /// </summary>
    public async Task<Dictionary<string, string>?> GetThreadMetadataAsync(string threadId)
    {
        try
        {
            var workItem = await GetAgentWorkItemAsync(threadId);
            return workItem?.Metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to get thread metadata");
            return null;
        }
    }

    /// <summary>
    /// Gets participants of a chat thread
    /// Note: This is a simplified implementation. In a full implementation,
    /// you would query ACS Chat SDK for actual participants.
    /// </summary>
    public async Task<List<ParticipantInfo>?> GetThreadParticipantsAsync(string threadId)
    {
        try
        {
            var workItem = await GetAgentWorkItemAsync(threadId);
            if (workItem == null)
            {
                return null;
            }

            // Return basic participant info from work item
            var participants = new List<ParticipantInfo>();

            // Add customer
            if (!string.IsNullOrEmpty(workItem.CustomerId))
            {
                participants.Add(new ParticipantInfo
                {
                    Identifier = workItem.CustomerId,
                    DisplayName = workItem.CustomerName
                });
            }

            // Add assigned agent if any
            if (!string.IsNullOrEmpty(workItem.AssignedAgentId))
            {
                participants.Add(new ParticipantInfo
                {
                    Identifier = workItem.AssignedAgentId,
                    DisplayName = workItem.AssignedAgentName
                });
            }

            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Repository] Failed to get thread participants");
            return null;
        }
    }

    #endregion
}

