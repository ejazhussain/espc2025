// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Agent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace O365C.ACS.Integration.API.Functions
{
    /// <summary>
    /// Agent-related Azure Function endpoints
    /// Handles agent assignment, lookup, and management via HTTP requests
    /// </summary>
    public class AgentFunction
    {
        private readonly IAgentService _agentService;
        private readonly ILogger<AgentFunction> _logger;
        private readonly IQueueService _queueService;

        public AgentFunction(IAgentService agentService, ILogger<AgentFunction> logger, IQueueService queueService)
        {
            _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        }

        /// <summary>
        /// API endpoint to get ACS user info for a given Teams user ID
        /// Maps Teams user identity to corresponding ACS user for chat operations
        /// GET /api/agent/getAgentUser?teamsUserId={teamsUserId}
        /// </summary>
        [Function("GetAgentUser")]
        public async Task<IActionResult> GetAgentAcsUserAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agent/getAgentUser")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to get agent ACS user");

                string? teamsUserId = req.Query["teamsUserId"];

                if (string.IsNullOrWhiteSpace(teamsUserId))
                {
                    _logger.LogWarning("[ACS Agent] TeamsUserId parameter is missing or empty");
                    return new BadRequestObjectResult(new
                    {
                        error = "TeamsUserId is required",
                        message = "Please provide a valid teamsUserId parameter"
                    });
                }

                _logger.LogInformation("[ACS Agent] Processing request for Teams user: {TeamsUserId}", teamsUserId);

                // Call the AgentService.GetAgentAcsUserAsync method
                var agentUser = await _agentService.GetAgentAcsUserAsync(teamsUserId);

                if (agentUser == null)
                {
                    _logger.LogWarning("[ACS Agent] No ACS user found for Teams user: {TeamsUserId}", teamsUserId);
                    return new NotFoundObjectResult(new
                    {
                        error = "Agent not found",
                        message = $"No linked ACS user found for TeamsUserId: {teamsUserId}"
                    });
                }

                _logger.LogInformation("[ACS Agent] Successfully retrieved ACS user {AcsUserId} for Teams user {TeamsUserId}",
                    agentUser.AcsUserId, teamsUserId);

                return new OkObjectResult(agentUser);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "[ACS Agent] Invalid argument provided");
                return new BadRequestObjectResult(new
                {
                    error = "Invalid parameter",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error processing GetAgentAcsUser request");
                return new ObjectResult(new
                {
                    error = "Internal server error",
                    message = "An error occurred while processing your request"
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Assigns a support agent to a chat thread
        /// POST /api/agent/assign
        /// 
        /// Body: AssignAgentRequest with threadId, priority, and department
        /// Returns: AssignAgentResponse with agent information
        /// </summary>
        [Function("AssignAgent")]
        public async Task<IActionResult> AssignAgentAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent/assignAgentUser")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to assign agent");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<AssignAgentRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request == null || string.IsNullOrWhiteSpace(request.ThreadId))
                {
                    return new BadRequestObjectResult(new AssignAgentResponse
                    {
                        Success = false,
                        ErrorMessage = "ThreadId is required"
                    });
                }

                // Assign an agent using dedicated agent service
                var response = await _agentService.AssignAgentAsync(request);

                _logger.LogInformation("[ACS Agent] Agent assignment result: Success={Success}, AgentName={AgentName}",
                    response.Success, response.AgentDisplayName?.MaskForLogging());

                return response.Success ? new OkObjectResult(response) : new BadRequestObjectResult(response);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ACS Agent] Invalid JSON in request body");
                return new BadRequestObjectResult(new AssignAgentResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid request format"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Unexpected error during agent assignment");
                return new ObjectResult(new AssignAgentResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                })
                {
                    StatusCode = 500
                };
            }
        }

        /// <summary>
        /// Gets all agent work items from the database
        /// GET /api/agent/workItems
        /// </summary>
        [Function("GetAgentWorkItems")]
        public async Task<IActionResult> GetAgentWorkItemsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agent/getAgentWorkItems")] HttpRequest req)
        {
            try
            {
                // Get optional status filter from query parameter
                string? statusParam = req.Query["status"];
                AgentWorkItemStatus? statusFilter = null;
                
                if (!string.IsNullOrEmpty(statusParam) && int.TryParse(statusParam, out int statusValue))
                {
                    statusFilter = (AgentWorkItemStatus)statusValue;
                    _logger.LogInformation("[ACS Agent] Filtering work items by status: {Status}", statusFilter);
                }
                else
                {
                    _logger.LogInformation("[ACS Agent] Received request to get all agent work items");
                }

                var workItems = await _agentService.GetAgentWorkItemsAsync(statusFilter);

                _logger.LogInformation("[ACS Agent] Successfully retrieved {Count} agent work items", workItems.Count());

                return new OkObjectResult(workItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error retrieving agent work items");
                return new BadRequestObjectResult(new
                {
                    error = "Failed to retrieve agent work items",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a new agent work item in the database
        /// POST /api/agent/workItems
        /// Body: { "id": "thread_id", "status": "active|resolved" }
        /// </summary>
        [Function("CreateAgentWorkItem")]
        public async Task<IActionResult> CreateAgentWorkItemAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent/createAgentWorkItems")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to create agent work item");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var workItemRequest = JsonSerializer.Deserialize<CreateAgentWorkItemRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (workItemRequest == null || string.IsNullOrWhiteSpace(workItemRequest.Id))
                {
                    _logger.LogWarning("[ACS Agent] Invalid request body for creating agent work item");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid request",
                        message = "ThreadId (id) is required"
                    });
                }

                // Create the AgentWorkItem with the provided data
                var workItem = new AgentWorkItem
                {
                    Id = workItemRequest.Id,
                    Status = workItemRequest.Status
                };

                var createdWorkItem = await _agentService.CreateAgentWorkItemAsync(workItem);

                _logger.LogInformation("[ACS Agent] Successfully created agent work item for thread {ThreadId}", workItemRequest.Id);

                return new OkObjectResult(createdWorkItem);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ACS Agent] Invalid JSON in request body");
                return new BadRequestObjectResult(new
                {
                    error = "Invalid JSON",
                    message = "Request body must be valid JSON"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error creating agent work item");
                return new BadRequestObjectResult(new
                {
                    error = "Failed to create agent work item",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates an existing agent work item status
        /// PUT /api/agent/workItems/{threadId}
        /// Body: { "status": "active|resolved" }
        /// </summary>
        [Function("UpdateAgentWorkItem")]
        public async Task<IActionResult> UpdateAgentWorkItemAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "agent/updateAgentWorkItems/{threadId}")] HttpRequest req,
            string threadId)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to update agent work item for thread {ThreadId}", threadId);

                if (string.IsNullOrWhiteSpace(threadId))
                {
                    _logger.LogWarning("[ACS Agent] ThreadId parameter is missing or empty");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid parameter",
                        message = "ThreadId is required"
                    });
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateRequest = JsonSerializer.Deserialize<UpdateAgentWorkItemRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (updateRequest == null)
                {
                    _logger.LogWarning("[ACS Agent] Invalid request body for updating agent work item");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid request",
                        message = "Status is required in request body"
                    });
                }

                var updatedWorkItem = await _agentService.UpdateAgentWorkItemAsync(threadId, updateRequest.Status);

                _logger.LogInformation("[ACS Agent] Successfully updated agent work item for thread {ThreadId} to status {Status}", 
                    threadId, updateRequest.Status);

                return new OkObjectResult(updatedWorkItem);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ACS Agent] Invalid JSON in request body");
                return new BadRequestObjectResult(new
                {
                    error = "Invalid JSON",
                    message = "Request body must be valid JSON"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error updating agent work item for thread {ThreadId}", threadId);
                return new BadRequestObjectResult(new
                {
                    error = "Failed to update agent work item",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Atomically claims a work item for an agent (prevents race conditions)
        /// POST /api/agent/claimWorkItem/{threadId}
        /// Body: { "agentId": "acs-user-id", "agentName": "Agent Name" }
        /// </summary>
        [Function("ClaimWorkItem")]
        public async Task<IActionResult> ClaimWorkItemAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent/claimWorkItem/{threadId}")] HttpRequest req,
            string threadId)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to claim work item {ThreadId}", threadId);

                if (string.IsNullOrWhiteSpace(threadId))
                {
                    _logger.LogWarning("[ACS Agent] ThreadId parameter is missing or empty");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid parameter",
                        message = "ThreadId is required"
                    });
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var claimRequest = JsonSerializer.Deserialize<ClaimWorkItemRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (claimRequest == null || string.IsNullOrWhiteSpace(claimRequest.AgentId) || string.IsNullOrWhiteSpace(claimRequest.AgentName))
                {
                    _logger.LogWarning("[ACS Agent] Invalid claim request body");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid request",
                        message = "AgentId and AgentName are required"
                    });
                }

                var result = await _agentService.ClaimWorkItemAsync(threadId, claimRequest.AgentId, claimRequest.AgentName);

                if (result.Success)
                {
                    _logger.LogInformation("[ACS Agent] Successfully claimed work item {ThreadId} for agent {AgentName}",
                        threadId, claimRequest.AgentName);

                    // Send queue message for SignalR broadcasting
                    await _queueService.SendChatClaimedAsync(threadId, claimRequest.AgentId, claimRequest.AgentName);

                    return new OkObjectResult(result);
                }
                else
                {
                    _logger.LogWarning("[ACS Agent] Failed to claim work item {ThreadId}: {Error}", threadId, result.Error);
                    return new ConflictObjectResult(result);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ACS Agent] Invalid JSON in request body");
                return new BadRequestObjectResult(new
                {
                    error = "Invalid JSON",
                    message = "Request body must be valid JSON"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error claiming work item {ThreadId}", threadId);
                return new ObjectResult(new
                {
                    error = "Failed to claim work item",
                    message = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Gets all unassigned work items (in the queue)
        /// GET /api/agent/getUnassignedWorkItems
        /// </summary>
        [Function("GetUnassignedWorkItems")]
        public async Task<IActionResult> GetUnassignedWorkItemsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agent/getUnassignedWorkItems")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to get unassigned work items");

                var workItems = await _agentService.GetUnassignedWorkItemsAsync();

                _logger.LogInformation("[ACS Agent] Successfully retrieved {Count} unassigned work items", workItems.Count());

                return new OkObjectResult(new
                {
                    items = workItems,
                    totalCount = workItems.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error retrieving unassigned work items");
                return new ObjectResult(new
                {
                    error = "Failed to retrieve unassigned work items",
                    message = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Gets all work items assigned to a specific agent
        /// GET /api/agent/getMyWorkItems/{agentId}?status=claimed|active|resolved
        /// </summary>
        [Function("GetMyWorkItems")]
        public async Task<IActionResult> GetMyWorkItemsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agent/getMyWorkItems/{agentId}")] HttpRequest req,
            string agentId)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to get work items for agent {AgentId}", agentId);

                if (string.IsNullOrWhiteSpace(agentId))
                {
                    _logger.LogWarning("[ACS Agent] AgentId parameter is missing or empty");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid parameter",
                        message = "AgentId is required"
                    });
                }

                // Parse optional status filter
                AgentWorkItemStatus? statusFilter = null;
                if (req.Query.TryGetValue("status", out var statusValue))
                {
                    if (Enum.TryParse<AgentWorkItemStatus>(statusValue, true, out var parsedStatus))
                    {
                        statusFilter = parsedStatus;
                    }
                }

                var workItems = await _agentService.GetWorkItemsByAgentIdAsync(agentId, statusFilter);

                _logger.LogInformation("[ACS Agent] Successfully retrieved {Count} work items for agent {AgentId}",
                    workItems.Count(), agentId);

                return new OkObjectResult(new
                {
                    items = workItems,
                    totalCount = workItems.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error retrieving work items for agent {AgentId}", agentId);
                return new ObjectResult(new
                {
                    error = "Failed to retrieve work items",
                    message = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Deletes/Cancels a work item (used when agent cancels or customer ends chat)
        /// DELETE /api/agent/deleteWorkItem/{threadId}
        /// Now marks as Cancelled instead of deleting for better tracking
        /// </summary>
        [Function("DeleteWorkItem")]
        public async Task<IActionResult> DeleteWorkItemAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "agent/deleteWorkItem/{threadId}")] HttpRequest req,
            string threadId)
        {
            try
            {
                _logger.LogInformation("[ACS Agent] Received request to cancel work item {ThreadId}", threadId);

                if (string.IsNullOrWhiteSpace(threadId))
                {
                    _logger.LogWarning("[ACS Agent] ThreadId parameter is missing or empty");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid parameter",
                        message = "ThreadId is required"
                    });
                }

                // Instead of deleting, update status to Cancelled for better tracking
                var result = await _agentService.CancelWorkItemAsync(threadId);

                if (result)
                {
                    _logger.LogInformation("[ACS Agent] Successfully cancelled work item {ThreadId}", threadId);
                    
                    // Send SignalR notification to update all agents in real-time
                    try
                    {
                        await _queueService.SendWorkItemCancelledAsync(threadId);
                        _logger.LogInformation("[ACS Agent] SignalR notification sent for cancelled work item {ThreadId}", threadId);
                    }
                    catch (Exception signalREx)
                    {
                        // Log but don't fail the request if SignalR fails
                        _logger.LogWarning(signalREx, "[ACS Agent] Failed to send SignalR notification for cancelled work item {ThreadId}", threadId);
                    }
                    
                    return new OkObjectResult(new
                    {
                        success = true,
                        message = $"Work item {threadId} cancelled successfully"
                    });
                }
                else
                {
                    // Work item not found - treat as success since the goal (removal) is already achieved
                    // This can happen if agent already resolved the chat or it was previously cancelled
                    _logger.LogInformation("[ACS Agent] Work item {ThreadId} not found - treating as successful cancellation", threadId);
                    return new OkObjectResult(new
                    {
                        success = true,
                        message = $"Work item {threadId} already removed or resolved"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Agent] Error deleting work item {ThreadId}", threadId);
                return new ObjectResult(new
                {
                    error = "Failed to delete work item",
                    message = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}