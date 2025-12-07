// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Communication.Identity;
using Azure.Communication.Chat;
using Azure.Communication;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Chat;
using O365C.ACS.Integration.API.Models.Agent;
using O365C.ACS.Integration.API.Models.Admin;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Helpers;
using O365C.ACS.Integration.API.Constants;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Azure Communication Services chat and thread management implementation
/// Simplified implementation following TypeScript patterns with persistent admin user
/// </summary>
public class ChatService : IChatService
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly ILogger<ChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChatRepository _chatRepository;
    private readonly AppSettings _appSettings;

    public ChatService(
        IConfiguration configuration,
        ILogger<ChatService> logger,
        IChatRepository chatRepository,
        AppSettings appSettings)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        try
        {
            var connectionString = GetAcsConnectionString();
            _identityClient = new CommunicationIdentityClient(connectionString);

            _logger.LogInformation("[Chat Service] Successfully initialized with connection string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Failed to initialize Communication Identity Client");
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

    /// <summary>
    /// Gets or creates a persistent admin user for chat operations
    /// This ensures we always use the same admin user to avoid permission issues
    /// </summary>
    public async Task<(string userId, string accessToken)> GetOrCreateAdminUserAsync()
    {
        try
        {
            // Try to get existing admin user from database
            var existingAdminUser = await _chatRepository.GetAdminUserAsync();

            if (existingAdminUser != null)
            {
                _logger.LogInformation("[Chat Service] Using existing admin user {AdminUserId}",
                    existingAdminUser.AcsUserId);

                // Create a new access token for the existing admin user
                var userIdentifier = new CommunicationUserIdentifier(existingAdminUser.AcsUserId);
                var tokenResponse = await _identityClient.GetTokenAsync(userIdentifier, new[] { CommunicationTokenScope.Chat });

                // Update last used timestamp
                await _chatRepository.UpdateAdminUserLastUsedAsync(existingAdminUser.Id);

                return (existingAdminUser.AcsUserId, tokenResponse.Value.Token);
            }

            // Create new admin user if none exists
            _logger.LogInformation("[Chat Service] Creating new admin user");

            var adminUserResponse = await _identityClient.CreateUserAsync();
            var adminUserId = adminUserResponse.Value.Id;

            // Create access token for the new admin user
            var newTokenResponse = await _identityClient.GetTokenAsync(adminUserResponse.Value, new[] { CommunicationTokenScope.Chat });

            // Save admin user to database using existing structure
            var adminUser = new AdminUser
            {
                Id = Guid.NewGuid().ToString(), // Generate unique ID instead of "system-admin"
                AcsUserId = adminUserId,
                DisplayName = "System Admin",
                Environment = "development", // Match the existing structure
                CreatedAt = DateTimeOffset.UtcNow,
                LastUsedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            await _chatRepository.SaveAdminUserAsync(adminUser);

            _logger.LogInformation("[Chat Service] Created and saved new admin user {AdminUserId}", adminUserId);

            return (adminUserId, newTokenResponse.Value.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Failed to get or create admin user");
            throw;
        }
    }

    public async Task<CreateThreadResponse> CreateThreadAsync(CreateThreadRequest request)
    {
        try
        {
            // Validate input (simplified validation)
            if (string.IsNullOrWhiteSpace(request?.DisplayName))
            {
                return new CreateThreadResponse
                {
                    Success = false,
                    ErrorMessage = "Display name is required"
                };
            }

            _logger.LogInformation("[Chat Service] Creating chat thread for user {DisplayName}",
                request.DisplayName);

            // Get or create persistent admin user
            var (adminUserId, accessToken) = await GetOrCreateAdminUserAsync();

            // Create chat client using the persistent admin user
            var chatClient = new ChatClient(new Uri(GetAcsEndpointUrl()),
                new CommunicationTokenCredential(accessToken));

            // Generate thread topic with improved fallback logic
            var threadTopic = !string.IsNullOrWhiteSpace(request.Topic)
                ? request.Topic
                : $"{request.DisplayName.Trim()} - General Support ({DateTime.Now:MMM dd HH:mm})";

            // Create the chat thread request
            var participants = new List<ChatParticipant>
            {
                new ChatParticipant(new CommunicationUserIdentifier(adminUserId))
                {
                    DisplayName = "System Admin"
                }
            };

            // Create the chat thread
            var result = await chatClient.CreateChatThreadAsync(threadTopic, participants);
            var threadId = result.Value.ChatThread?.Id;

            if (string.IsNullOrEmpty(threadId))
            {
                throw new InvalidOperationException("Invalid or missing ID for newly created thread");
            }

            // Save agent work item to database as UNASSIGNED (enters the queue)
            await SaveAgentWorkItemToDatabaseAsync(threadId, AgentWorkItemStatus.Unassigned, request.DisplayName, request.UserId);

            _logger.LogInformation("[Chat Service] Successfully created chat thread {ThreadId} with topic '{Topic}'",
                threadId, threadTopic);

            return new CreateThreadResponse
            {
                ThreadId = threadId,
                Success = true
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[Chat Service] Azure request failed while creating chat thread. Status: {Status}, Code: {Code}",
                ex.Status, ex.ErrorCode);

            return new CreateThreadResponse
            {
                Success = false,
                ErrorMessage = $"Failed to create chat thread: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Unexpected error while creating chat thread");

            return new CreateThreadResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while creating the chat thread"
            };
        }
    }

    /// <summary>
    /// Saves an agent work item to the database
    /// Creates work items with UNASSIGNED status so they enter the queue
    /// </summary>
    private async Task SaveAgentWorkItemToDatabaseAsync(string threadId, AgentWorkItemStatus status, string customerName = "", string? customerId = null)
    {
        try
        {
            var workItem = new AgentWorkItem
            {
                Id = threadId,
                Status = status,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Unknown Customer" : customerName,
                CustomerId = customerId,
                AssignedAgentId = null,
                AssignedAgentName = null,
                ClaimedAt = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _chatRepository.CreateAgentWorkItemAsync(workItem);
            _logger.LogInformation("[Chat Service] Successfully saved agent work item for thread {ThreadId} with status {Status} and customer {CustomerName}",
                threadId, status, customerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Failed to save agent work item for thread {ThreadId}", threadId);
            // Don't throw - this is a non-critical operation, matching TypeScript behavior
        }
    }

    /// <summary>
    /// Determines the appropriate history share time based on user role
    /// This controls which historical messages participants can see
    /// </summary>
    /// <param name="role">User role: "customer", "agent", "supervisor", "member"</param>
    /// <returns>DateTimeOffset indicating from when the user can see chat history</returns>
    private static DateTimeOffset DetermineHistoryShareTime(string role)
    {
        // Everyone sees all message history including AI conversation context
        // This provides full transparency and context for all participants
        return DateTimeOffset.MinValue;
    }

    public async Task<JoinThreadResponse> JoinThreadAsync(JoinThreadRequest request)
    {
        try
        {
            // Validate input (simplified validation)
            if (string.IsNullOrWhiteSpace(request?.ThreadId) ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new JoinThreadResponse
                {
                    Success = false,
                    ThreadId = request?.ThreadId ?? string.Empty,
                    UserId = request?.UserId ?? string.Empty,
                    ErrorMessage = "Missing required parameters"
                };
            }

            _logger.LogInformation("[Chat Service] Adding user {DisplayName} to thread {ThreadId}",
                request.DisplayName, request.ThreadId);

            // Get or create persistent admin user (reuse the same one from thread creation)
            var (adminUserId, accessToken) = await GetOrCreateAdminUserAsync();

            // Create chat client using the persistent admin user
            var chatClient = new ChatClient(new Uri(GetAcsEndpointUrl()),
                new CommunicationTokenCredential(accessToken));

            // Get the chat thread client
            var chatThreadClient = chatClient.GetChatThreadClient(request.ThreadId);

            // Determine history sharing based on user role
            // Agents see all history (including escalated conversation from AI)
            // Customers only see messages from when they join (no historical context)
            var shareHistoryTime = DetermineHistoryShareTime(request.Role);

            _logger.LogInformation("[Chat Service] Adding user {DisplayName} with role '{Role}' - ShareHistoryTime: {ShareHistoryTime}",
                request.DisplayName, request.Role, shareHistoryTime);

            // Add the participant to the thread
            var participants = new[]
            {
                new ChatParticipant(new CommunicationUserIdentifier(request.UserId))
                {
                    DisplayName = request.DisplayName,
                    ShareHistoryTime = shareHistoryTime
                }
            };

            await chatThreadClient.AddParticipantsAsync(participants);

            _logger.LogInformation("[Chat Service] Successfully added user to thread {ThreadId} with role {Role}", 
                request.ThreadId, request.Role);

            return new JoinThreadResponse
            {
                Success = true,
                ThreadId = request.ThreadId,
                UserId = request.UserId,
                DisplayName = request.DisplayName,
                Role = request.Role,
                ShareHistoryTime = shareHistoryTime
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[Chat Service] ACS error while adding user to thread");

            return new JoinThreadResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                UserId = request?.UserId ?? string.Empty,
                ErrorMessage = "Failed to add user to thread",
                ErrorCode = ex.ErrorCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Unexpected error while adding user to thread");

            return new JoinThreadResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                UserId = request?.UserId ?? string.Empty,
                ErrorMessage = "An unexpected error occurred while adding user to thread"
            };
        }
    }

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        try
        {
            // Validate input (simplified validation)
            if (string.IsNullOrWhiteSpace(request?.ThreadId) ||
                string.IsNullOrWhiteSpace(request.Message) ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new SendMessageResponse
                {
                    Success = false,
                    ThreadId = request?.ThreadId ?? string.Empty,
                    SenderDisplayName = request?.DisplayName ?? string.Empty,
                    MessageContent = request?.Message ?? string.Empty,
                    ErrorMessage = "Missing required parameters"
                };
            }

            _logger.LogInformation("[Chat Service] Sending message to thread {ThreadId} from user {DisplayName}",
                request.ThreadId, request.DisplayName);


            var sender = new CommunicationUserIdentifier(request.UserId);

            // Get token for the sender (similar to TypeScript createChatThreadClient pattern)
            var tokenResponse = await _identityClient.GetTokenAsync(sender, new[] { CommunicationTokenScope.Chat });
            var accessToken = tokenResponse.Value.Token;

            // Create chat thread client (equivalent to TypeScript: createChatThreadClient(sender, threadId))
            var chatClient = new ChatClient(new Uri(GetAcsEndpointUrl()),
                new CommunicationTokenCredential(accessToken));
            var chatThreadClient = chatClient.GetChatThreadClient(request.ThreadId);

            // Send message (equivalent to TypeScript: chatThreadClient.sendMessage({ content }, { senderDisplayName }))
            // Determine message type based on request (default to Text)
            var messageType = request.MessageType?.ToLowerInvariant() == "html" 
                ? ChatMessageType.Html 
                : ChatMessageType.Text;

            var sendMessageOptions = new SendChatMessageOptions
            {
                Content = request.Message.Trim(),
                MessageType = messageType,
                SenderDisplayName = request.DisplayName
            };

            var sendResult = await chatThreadClient.SendMessageAsync(sendMessageOptions);
            var messageId = sendResult.Value.Id;

            _logger.LogInformation("[Chat Service] Successfully sent message {MessageId} to thread {ThreadId}",
                messageId, request.ThreadId);

            return new SendMessageResponse
            {
                Success = true,
                MessageId = messageId,
                ThreadId = request.ThreadId,
                SenderDisplayName = request.DisplayName,
                MessageContent = request.Message
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[Chat Service] Azure request failed while sending message to thread {ThreadId}. Status: {Status}, Code: {Code}",
                request?.ThreadId, ex.Status, ex.ErrorCode);

            return new SendMessageResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                SenderDisplayName = request?.DisplayName ?? string.Empty,
                MessageContent = request?.Message ?? string.Empty,
                ErrorMessage = $"Failed to send message: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Unexpected error while sending message to thread {ThreadId}",
                request?.ThreadId);

            return new SendMessageResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                SenderDisplayName = request?.DisplayName ?? string.Empty,
                MessageContent = request?.Message ?? string.Empty,
                ErrorMessage = "An unexpected error occurred while sending the message"
            };
        }
    }

    public async Task<SendConversationHistoryResponse> SendConversationHistoryAsync(SendConversationHistoryRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request?.ThreadId) ||
                request.ConversationHistory == null ||
                request.ConversationHistory.Count == 0)
            {
                return new SendConversationHistoryResponse
                {
                    Success = false,
                    ThreadId = request?.ThreadId ?? string.Empty,
                    ErrorMessage = "ThreadId and ConversationHistory are required"
                };
            }

            _logger.LogInformation("[Chat Service] Sending {MessageCount} conversation history messages to thread {ThreadId}",
                request.ConversationHistory.Count, request.ThreadId.MaskForLogging());

            // Get admin user for sending system messages
            var (adminUserId, accessToken) = await GetOrCreateAdminUserAsync();
            
            // Create chat thread client using admin user
            var chatClient = new ChatClient(new Uri(GetAcsEndpointUrl()),
                new CommunicationTokenCredential(accessToken));
            var chatThreadClient = chatClient.GetChatThreadClient(request.ThreadId);

            int messagesSent = 0;
            int totalMessages = request.ConversationHistory.Count;

            // Send each conversation message WITHOUT headers/footers
            // Since ShareHistoryTime = MinValue, everyone sees all messages naturally
            foreach (var historyMessage in request.ConversationHistory)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(historyMessage.Content))
                    {
                        continue; // Skip empty messages
                    }

                    var sendMessageOptions = new SendChatMessageOptions
                    {
                        Content = historyMessage.Content.Trim(),
                        MessageType = ChatMessageType.Text,
                        SenderDisplayName = historyMessage.SenderDisplayName
                    };

                    await chatThreadClient.SendMessageAsync(sendMessageOptions);
                    messagesSent++;

                    _logger.LogDebug("[Chat Service] Sent conversation history message {MessageIndex}/{TotalMessages} from {SenderName}",
                        messagesSent, totalMessages, historyMessage.SenderDisplayName);

                    // Small delay to maintain message order
                    await Task.Delay(100);
                }
                catch (Exception messageEx)
                {
                    _logger.LogWarning(messageEx, "[Chat Service] Failed to send individual conversation history message from {SenderName}",
                        historyMessage.SenderDisplayName);
                    // Continue with other messages
                }
            }

            _logger.LogInformation("[Chat Service] Successfully sent {MessagesSent}/{TotalMessages} conversation history messages to thread {ThreadId}",
                messagesSent, totalMessages, request.ThreadId.MaskForLogging());

            return new SendConversationHistoryResponse
            {
                Success = true,
                ThreadId = request.ThreadId,
                MessagesSent = messagesSent,
                TotalMessages = totalMessages,
                Details = $"Sent {messagesSent} conversation history messages"
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[Chat Service] Azure request failed while sending conversation history to thread {ThreadId}. Status: {Status}, Code: {Code}",
                request?.ThreadId, ex.Status, ex.ErrorCode);

            return new SendConversationHistoryResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                ErrorMessage = $"Failed to send conversation history: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Unexpected error while sending conversation history to thread {ThreadId}",
                request?.ThreadId);

            return new SendConversationHistoryResponse
            {
                Success = false,
                ThreadId = request?.ThreadId ?? string.Empty,
                ErrorMessage = "An unexpected error occurred while sending conversation history"
            };
        }
    }

    public async Task<IReadOnlyList<Models.Chat.ChatThreadMessage>> GetThreadMessagesAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("ThreadId is required", nameof(threadId));
        }

        try
        {
            _logger.LogInformation("[Chat Service] Retrieving messages for thread {ThreadId}", threadId.MaskForLogging());

            var (_, accessToken) = await GetOrCreateAdminUserAsync();
            var chatClient = new ChatClient(new Uri(GetAcsEndpointUrl()), new CommunicationTokenCredential(accessToken));
            var chatThreadClient = chatClient.GetChatThreadClient(threadId);

            var messages = new List<Models.Chat.ChatThreadMessage>();

            await foreach (var message in chatThreadClient.GetMessagesAsync())
            {
                if (message.Type != ChatMessageType.Text && message.Type != ChatMessageType.Html)
                {
                    continue;
                }

                var content = message.Content?.Message?.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                messages.Add(new ChatThreadMessage
                {
                    Id = message.Id ?? Guid.NewGuid().ToString(),
                        SenderId = message.SenderDisplayName ?? string.Empty,
                        SenderDisplayName = string.IsNullOrWhiteSpace(message.SenderDisplayName)
                            ? "Unknown"
                            : message.SenderDisplayName!,
                    Content = content,
                    SentAtUtc = message.CreatedOn,
                    Type = message.Type.ToString()
                });
            }

            var ordered = messages
                .OrderBy(m => m.SentAtUtc)
                .ToList();

            _logger.LogInformation("[Chat Service] Retrieved {Count} messages for thread {ThreadId}", ordered.Count, threadId.MaskForLogging());

            return ordered;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[Chat Service] Failed to retrieve messages for thread {ThreadId}. Status: {Status}, Code: {Code}",
                threadId.MaskForLogging(), ex.Status, ex.ErrorCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Unexpected error while retrieving messages for thread {ThreadId}",
                threadId.MaskForLogging());
            throw;
        }
    }

    public async Task<bool> RemoveUserFromThreadAsync(string threadId, string userId)
    {
        // TODO: Implement - migrate RemoveUserFromThreadAsync implementation
        await Task.CompletedTask; // Suppress warning
        throw new NotImplementedException("Method implementation pending migration");
    }

    public async Task<CreateThreadResponse?> GetThreadInfoAsync(string threadId)
    {
        // TODO: Implement - migrate GetThreadInfoAsync implementation
        await Task.CompletedTask; // Suppress warning
        throw new NotImplementedException("Method implementation pending migration");
    }

    /// <summary>
    /// Deletes a chat thread and all associated data including CosmosDB records
    /// </summary>
    public async Task<bool> DeleteThreadAsync(string threadId)
    {
        threadId.ValidateNotNullOrEmpty(nameof(threadId));

        try
        {
            _logger.LogInformation("[Chat Service] Starting thread deletion for {ThreadId}", 
                threadId.MaskForLogging());

            // Get admin user credentials for ACS operations
            var (adminUserId, adminToken) = await GetOrCreateAdminUserAsync();

            // Create chat client with admin credentials
            var endpointUrl = GetAcsEndpointUrl();
            var chatClient = new ChatClient(new Uri(endpointUrl), new CommunicationTokenCredential(adminToken));

            // Step 1: Delete the ACS thread (this also deletes all messages)
            try
            {
                await chatClient.DeleteChatThreadAsync(threadId);
                _logger.LogInformation("[Chat Service] Successfully deleted ACS thread {ThreadId}", 
                    threadId.MaskForLogging());
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("[Chat Service] ACS thread {ThreadId} not found, may already be deleted", 
                    threadId.MaskForLogging());
                // Continue with cleanup even if ACS thread doesn't exist
            }

            // Step 2: Delete the agent work item from CosmosDB
            var deleted = await _chatRepository.DeleteAgentWorkItemAsync(threadId);
            if (deleted)
            {
                _logger.LogInformation("[Chat Service] Successfully deleted CosmosDB work item for {ThreadId}", 
                    threadId.MaskForLogging());
            }
            else
            {
                _logger.LogWarning("[Chat Service] CosmosDB work item for {ThreadId} not found or already deleted", 
                    threadId.MaskForLogging());
            }

            // TODO: Add any additional cleanup here if needed
            // - Delete related files/attachments
            // - Clean up Teams notifications
            // - Remove from any caches

            _logger.LogInformation("[Chat Service] Successfully completed thread deletion for {ThreadId}", 
                threadId.MaskForLogging());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Chat Service] Failed to delete thread {ThreadId}", 
                threadId.MaskForLogging());
            return false;
        }
    }
}
