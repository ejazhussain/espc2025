// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Chat;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace O365C.ACS.Integration.API;

/// <summary>
/// Azure Functions for Azure Communication Services chat operations
/// Implements secure chat thread management following Azure best practices
/// 
/// Features:
/// - Thread creation and management
/// - User thread joining
/// - Message sending
/// - Enterprise logging and monitoring
/// 
/// Reference: https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/chat/get-started
/// </summary>
public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly IChatService _chatService;
    private readonly IQueueService _queueService;
    private readonly ITeamsNotificationService _teamsNotificationService;
    private readonly AppSettings _appSettings;

    public ChatFunction(
        ILogger<ChatFunction> logger,
        IChatService chatService,
        IQueueService queueService,
        ITeamsNotificationService teamsNotificationService,
        IOptions<AppSettings> appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _teamsNotificationService = teamsNotificationService ?? throw new ArgumentNullException(nameof(teamsNotificationService));
        _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }

    /// <summary>
    /// Creates a new chat thread
    /// POST /api/chat/thread/create
    ///
    /// Body: CreateThreadRequest with displayName and optional topic
    /// Returns: CreateThreadResponse with threadId
    /// </summary>
    [Function("CreateThread")]
    public async Task<IActionResult> CreateThreadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/thread/create")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Received request to create chat thread");

            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("[ACS Chat] Request body: {RequestBody}", requestBody);

            var request = JsonSerializer.Deserialize<CreateThreadRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new CreateThreadResponse
                {
                    Success = false,
                    ErrorMessage = "Request body is required"
                });
            }

            _logger.LogInformation("[ACS Chat] Parsed request - DisplayName: {DisplayName}, Topic: {Topic}",
                request.DisplayName ?? "(null)", request.Topic ?? "(null)");

            // Create the chat thread using dedicated chat service
            var response = await _chatService.CreateThreadAsync(request);

            _logger.LogInformation("[ACS Chat] Thread creation result: Success={Success}, ThreadId={ThreadId}",
                response.Success, response.ThreadId?.MaskForLogging());

            // Send queue message for SignalR broadcasting if thread creation was successful
            if (response.Success && !string.IsNullOrEmpty(response.ThreadId))
            {
                var customerName = string.IsNullOrWhiteSpace(request.DisplayName) ? "Unknown Customer" : request.DisplayName;
                _logger.LogInformation("[ACS Chat] Sending queue message with customer name: {CustomerName}", customerName);
                
                // Send SignalR notification via queue
                await _queueService.SendNewChatRequestAsync(response.ThreadId, customerName);
                
                // Send Teams activity notification to Ejaz Hussain (primary agent)
                // Using hardcoded agent ID for now - can be made dynamic later
                const string ejazTeamsUserId = "2a5de346-1d63-4c7a-897f-b1f4b5316fe5";
                
                _logger.LogInformation("[ACS Chat] Sending Teams notification to Ejaz Hussain");
                var notificationSent = await _teamsNotificationService.SendNewChatNotificationToAgentAsync(
                    ejazTeamsUserId,
                    response,
                    customerName,
                    "NORMAL",
                    request.Topic // Use topic as initial message context
                );
                _logger.LogInformation("[ACS Chat] Teams notification: {Status}", 
                    notificationSent ? "Sent successfully" : "Failed");
            }

            return response.Success ? new OkObjectResult(response) : new BadRequestObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ACS Chat] Invalid JSON in request body");
            return new BadRequestObjectResult(new CreateThreadResponse
            {
                Success = false,
                ErrorMessage = "Invalid request format"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during thread creation");
            return new ObjectResult(new CreateThreadResponse
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
    /// Joins a user to an existing chat thread
    /// POST /api/acs/thread/join
    /// 
    /// Body: JoinThreadRequest with threadId, userId, and displayName
    /// Returns: JoinThreadResponse with success status
    /// </summary>
    [Function("JoinThread")]
    public async Task<IActionResult> JoinThreadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/thread/join")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Received request to join thread");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<JoinThreadRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null ||
                string.IsNullOrWhiteSpace(request.ThreadId) ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new BadRequestObjectResult(new JoinThreadResponse
                {
                    Success = false,
                    ErrorMessage = "ThreadId, UserId, and DisplayName are required"
                });
            }

            // Join the thread using dedicated chat service
            var response = await _chatService.JoinThreadAsync(request);

            _logger.LogInformation("[ACS Chat] Join thread result: Success={Success}, ThreadId={ThreadId}",
                response.Success, response.ThreadId?.MaskForLogging());

            return response.Success ? new OkObjectResult(response) : new BadRequestObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ACS Chat] Invalid JSON in request body");
            return new BadRequestObjectResult(new JoinThreadResponse
            {
                Success = false,
                ErrorMessage = "Invalid request format"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during thread join");
            return new ObjectResult(new JoinThreadResponse
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
    /// Sends a message to an existing chat thread
    /// POST /api/acs/message/send
    /// 
    /// Body: SendMessageRequest with userId, displayName, threadId, and message
    /// Returns: SendMessageResponse with message details
    /// </summary>
    [Function("SendMessage")]
    public async Task<IActionResult> SendMessageAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/message/send")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Received request to send message");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<SendMessageRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.DisplayName) ||
                string.IsNullOrWhiteSpace(request.ThreadId) ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                return new BadRequestObjectResult(new SendMessageResponse
                {
                    Success = false,
                    ThreadId = request?.ThreadId ?? string.Empty,
                    SenderDisplayName = request?.DisplayName ?? string.Empty,
                    MessageContent = request?.Message ?? string.Empty,
                    ErrorMessage = "UserId, DisplayName, ThreadId, and Message are required"
                });
            }

            // Send the message using dedicated chat service
            var response = await _chatService.SendMessageAsync(request);

            _logger.LogInformation("[ACS Chat] Send message result: Success={Success}, MessageId={MessageId}, ThreadId={ThreadId}",
                response.Success, response.MessageId?.MaskForLogging(), response.ThreadId?.MaskForLogging());

            return response.Success ? new OkObjectResult(response) : new BadRequestObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ACS Chat] Invalid JSON in request body");
            return new BadRequestObjectResult(new SendMessageResponse
            {
                Success = false,
                ThreadId = string.Empty,
                SenderDisplayName = string.Empty,
                MessageContent = string.Empty,
                ErrorMessage = "Invalid request format"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during message send");
            return new ObjectResult(new SendMessageResponse
            {
                Success = false,
                ThreadId = string.Empty,
                SenderDisplayName = string.Empty,
                MessageContent = string.Empty,
                ErrorMessage = "Internal server error"
            })
            {
                StatusCode = 500
            };
        }
    }    

    /// <summary>
    /// Sends conversation history from AI chat to ACS chat thread
    /// POST /api/chat/history/send
    /// 
    /// Body: SendConversationHistoryRequest with threadId and conversationHistory
    /// Returns: SendConversationHistoryResponse with success status
    /// </summary>
    [Function("SendConversationHistory")]
    public async Task<IActionResult> SendConversationHistoryAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/history/send")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Received request to send conversation history");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("[ACS Chat] Request body: {RequestBody}", requestBody);

            var request = JsonSerializer.Deserialize<SendConversationHistoryRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null ||
                string.IsNullOrWhiteSpace(request.ThreadId) ||
                request.ConversationHistory == null ||
                request.ConversationHistory.Count == 0)
            {
                return new BadRequestObjectResult(new SendConversationHistoryResponse
                {
                    Success = false,
                    ThreadId = request?.ThreadId ?? string.Empty,
                    ErrorMessage = "ThreadId and ConversationHistory are required"
                });
            }

            _logger.LogInformation("[ACS Chat] Sending {MessageCount} conversation history messages to thread {ThreadId}",
                request.ConversationHistory.Count, request.ThreadId.MaskForLogging());

            // Debug: Log each message content and length
            for (int i = 0; i < request.ConversationHistory.Count; i++)
            {
                var msg = request.ConversationHistory[i];
                _logger.LogInformation("[ACS Chat] Message {Index}: Sender={Sender}, ContentLength={Length}, Content={Content}",
                    i, msg.SenderDisplayName, msg.Content?.Length ?? 0, msg.Content);
            }

            // Send the conversation history using dedicated chat service
            var response = await _chatService.SendConversationHistoryAsync(request);

            _logger.LogInformation("[ACS Chat] Send conversation history result: Success={Success}, MessagesSent={MessagesSent}, ThreadId={ThreadId}",
                response.Success, response.MessagesSent, response.ThreadId?.MaskForLogging());

            return response.Success ? new OkObjectResult(response) : new BadRequestObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ACS Chat] Invalid JSON in conversation history request body");
            return new BadRequestObjectResult(new SendConversationHistoryResponse
            {
                Success = false,
                ThreadId = string.Empty,
                ErrorMessage = "Invalid request format"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during conversation history send");
            return new ObjectResult(new SendConversationHistoryResponse
            {
                Success = false,
                ThreadId = string.Empty,
                ErrorMessage = "Internal server error"
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Adds a user to a chat thread (simplified version matching frontend pattern)
    /// POST /api/acs/addUser/{threadId}
    /// 
    /// Body: { "Id": "user_id", "DisplayName": "display_name" }
    /// Returns: 201 Created on success, 404 Not Found on failure
    /// </summary>
    [Function("AddUser")]
    public async Task<IActionResult> AddUserAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/addUser/{threadId}")] HttpRequest req,
        string threadId)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Received request to add user to thread {ThreadId}", threadId.MaskForLogging());

            if (string.IsNullOrWhiteSpace(threadId))
            {
                return new BadRequestResult();
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var userData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (userData == null ||
                !userData.TryGetValue("id", out var userId) || string.IsNullOrWhiteSpace(userId) ||
                !userData.TryGetValue("displayName", out var displayName) || string.IsNullOrWhiteSpace(displayName))
            {
                _logger.LogWarning("[ACS Chat] User ID and DisplayName are required");
                return new BadRequestResult();
            }

            // Create a JoinThreadRequest for the chat service
            var joinRequest = new JoinThreadRequest
            {
                ThreadId = threadId,
                UserId = userId,
                DisplayName = displayName,
                Role = "customer"
            };

            // Join the thread using dedicated chat service
            var response = await _chatService.JoinThreadAsync(joinRequest);

            if (response.Success)
            {
                _logger.LogInformation("[ACS Chat] Successfully added user {DisplayName} to thread {ThreadId}",
                    displayName.MaskForLogging(), threadId.MaskForLogging());

                return new StatusCodeResult(201); // 201 Created to match TypeScript pattern
            }
            else
            {
                _logger.LogWarning("[ACS Chat] Failed to add user {DisplayName} to thread {ThreadId}",
                    displayName.MaskForLogging(), threadId.MaskForLogging());
                return new NotFoundResult(); // 404 to match TypeScript pattern
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ACS Chat] Invalid JSON in request body");
            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during add user operation");
            return new NotFoundResult(); // 404 to match TypeScript pattern
        }
    }

    /// <summary>
    /// Deletes a chat thread and all associated data
    /// DELETE /api/chat/thread/{threadId}
    /// 
    /// Returns: 200 OK on success, 404 if thread not found, 500 on error
    /// </summary>
    [Function("DeleteThread")]
    public async Task<IActionResult> DeleteThreadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "chat/thread/{threadId}")] HttpRequest req,
        string threadId)
    {
        try
        {
            _logger.LogInformation("[ACS Chat] Delete thread request received for {ThreadId}", 
                threadId?.MaskForLogging());

            // Validate thread ID
            if (string.IsNullOrEmpty(threadId))
            {
                _logger.LogWarning("[ACS Chat] Thread ID is required for deletion");
                return new BadRequestObjectResult(new { error = "Thread ID is required" });
            }

            // Call chat service to delete thread and all associated data
            var success = await _chatService.DeleteThreadAsync(threadId);

            if (success)
            {
                _logger.LogInformation("[ACS Chat] Successfully deleted thread {ThreadId}", 
                    threadId.MaskForLogging());
                
                return new OkObjectResult(new { 
                    success = true, 
                    threadId = threadId,
                    message = "Thread deleted successfully"
                });
            }
            else
            {
                _logger.LogWarning("[ACS Chat] Thread {ThreadId} not found for deletion", 
                    threadId.MaskForLogging());
                
                return new NotFoundObjectResult(new { 
                    success = false, 
                    threadId = threadId,
                    error = "Thread not found" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Chat] Unexpected error during thread deletion for {ThreadId}", 
                threadId?.MaskForLogging());
            
            return new ObjectResult(new { 
                success = false, 
                threadId = threadId,
                error = "An unexpected error occurred while deleting the thread" 
            })
            {
                StatusCode = 500
            };
        }
    }
}