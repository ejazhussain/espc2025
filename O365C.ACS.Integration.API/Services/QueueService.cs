using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Service for sending messages to Azure Storage Queues for SignalR broadcasting
/// </summary>
public class QueueService : IQueueService
{
    private readonly QueueClient _newChatRequestQueue;
    private readonly QueueClient _chatClaimedQueue;
    private readonly QueueClient _workItemCancelledQueue;
    private readonly ILogger<QueueService> _logger;

    public QueueService(IConfiguration configuration, ILogger<QueueService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connectionString = configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage connection string is not configured");

        var newChatRequestQueueName = configuration["NewChatRequestQueue"] ?? "new-chat-request-queue";
        var chatClaimedQueueName = configuration["ChatClaimedQueue"] ?? "chat-claimed-queue";
        var workItemCancelledQueueName = configuration["WorkItemCancelledQueue"] ?? "work-item-cancelled-queue";

        _logger.LogInformation("[Queue Service] Initializing queue clients");
        _logger.LogInformation("[Queue Service] New Chat Request Queue: {QueueName}", newChatRequestQueueName);
        _logger.LogInformation("[Queue Service] Chat Claimed Queue: {QueueName}", chatClaimedQueueName);
        _logger.LogInformation("[Queue Service] Work Item Cancelled Queue: {QueueName}", workItemCancelledQueueName);

        try
        {
            var options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.None // Disable Base64 encoding
            };

            _newChatRequestQueue = new QueueClient(connectionString, newChatRequestQueueName, options);
            _chatClaimedQueue = new QueueClient(connectionString, chatClaimedQueueName, options);
            _workItemCancelledQueue = new QueueClient(connectionString, workItemCancelledQueueName, options);

            // Create queues if they don't exist (safe for production - idempotent operation)
            _newChatRequestQueue.CreateIfNotExists();
            _chatClaimedQueue.CreateIfNotExists();
            _workItemCancelledQueue.CreateIfNotExists();

            _logger.LogInformation("[Queue Service] Queue clients initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Queue Service] Failed to initialize queue clients");
            throw;
        }
    }

    /// <summary>
    /// Sends a new chat request notification to the queue for SignalR broadcasting
    /// </summary>
    public async Task SendNewChatRequestAsync(string threadId, string customerName)
    {
        try
        {
            _logger.LogInformation("[Queue Service] Sending newChatRequest message for thread {ThreadId}", threadId);

            var message = new
            {
                eventType = "newChatRequest",
                workItem = new
                {
                    id = threadId,
                    customerName = customerName,
                    createdAt = DateTimeOffset.UtcNow,
                    status = 0, // Unassigned
                    priority = "NORMAL"
                }
            };

            var json = JsonSerializer.Serialize(message);
            _logger.LogInformation("[Queue Service] Serialized message: {Json}", json);
            await _newChatRequestQueue.SendMessageAsync(json);

            _logger.LogInformation("[Queue Service] Successfully sent newChatRequest message for thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Queue Service] Failed to send newChatRequest message for thread {ThreadId}", threadId);
            // Don't throw - we don't want queue failures to break the main API flow
            // SignalR notifications are nice-to-have, not critical
        }
    }

    /// <summary>
    /// Sends a chat claimed notification to the queue for SignalR broadcasting
    /// </summary>
    public async Task SendChatClaimedAsync(string threadId, string agentId, string agentName)
    {
        try
        {
            _logger.LogInformation("[Queue Service] Sending chatClaimed message for thread {ThreadId}, agent {AgentId}", threadId, agentId);

            var message = new
            {
                eventType = "chatClaimed",
                threadId = threadId,
                agentId = agentId,
                agentName = agentName,
                claimedAt = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(message);
            await _chatClaimedQueue.SendMessageAsync(json);

            _logger.LogInformation("[Queue Service] Successfully sent chatClaimed message for thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Queue Service] Failed to send chatClaimed message for thread {ThreadId}", threadId);
            // Don't throw - we don't want queue failures to break the main API flow
            // SignalR notifications are nice-to-have, not critical
        }
    }

    /// <summary>
    /// Sends a work item deleted notification to the queue for SignalR broadcasting
    /// </summary>
    public async Task SendWorkItemDeletedAsync(string threadId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(threadId))
            {
                _logger.LogWarning("[Queue Service] Cannot send workItemDeleted - threadId is empty");
                return;
            }

            _logger.LogInformation("[Queue Service] Sending workItemDeleted message for thread {ThreadId}", threadId);

            var message = new
            {
                eventType = "workItemDeleted",
                threadId = threadId,
                deletedAt = DateTimeOffset.UtcNow,
                reason = "Customer ended chat or agent canceled"
            };

            var json = JsonSerializer.Serialize(message);
            await _newChatRequestQueue.SendMessageAsync(json); // Use same queue for now

            _logger.LogInformation("[Queue Service] Successfully sent workItemDeleted message for thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Queue Service] Failed to send workItemDeleted message for thread {ThreadId}", threadId);
            // Don't throw - we don't want queue failures to break the main API flow
        }
    }

    /// <summary>
    /// Sends a work item cancelled notification to the queue for SignalR broadcasting
    /// </summary>
    public async Task SendWorkItemCancelledAsync(string threadId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(threadId))
            {
                _logger.LogWarning("[Queue Service] Cannot send workItemCancelled - threadId is empty");
                return;
            }

            _logger.LogInformation("[Queue Service] Sending workItemCancelled message for thread {ThreadId}", threadId);

            var message = new
            {
                eventType = "workItemCancelled",
                threadId = threadId,
                cancelledAt = DateTimeOffset.UtcNow,
                status = 4 // Cancelled status
            };

            var json = JsonSerializer.Serialize(message);
            await _workItemCancelledQueue.SendMessageAsync(json);

            _logger.LogInformation("[Queue Service] Successfully sent workItemCancelled message for thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Queue Service] Failed to send workItemCancelled message for thread {ThreadId}", threadId);
            // Don't throw - we don't want queue failures to break the main API flow
        }
    }
}
