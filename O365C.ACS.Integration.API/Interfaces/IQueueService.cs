namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Service for sending messages to Azure Storage Queues for SignalR broadcasting
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Sends a new chat request notification to the queue for SignalR broadcasting
    /// </summary>
    /// <param name="threadId">The thread/conversation ID</param>
    /// <param name="customerName">The customer's display name</param>
    /// <returns>Task representing the async operation</returns>
    Task SendNewChatRequestAsync(string threadId, string customerName);

    /// <summary>
    /// Sends a chat claimed notification to the queue for SignalR broadcasting
    /// </summary>
    /// <param name="threadId">The thread/conversation ID</param>
    /// <param name="agentId">The ACS user ID of the agent who claimed the chat</param>
    /// <param name="agentName">The display name of the agent who claimed the chat</param>
    /// <returns>Task representing the async operation</returns>
    Task SendChatClaimedAsync(string threadId, string agentId, string agentName);

    /// <summary>
    /// Sends a work item deleted notification to the queue for SignalR broadcasting
    /// (used when customer ends chat or agent cancels)
    /// </summary>
    /// <param name="threadId">The thread/conversation ID</param>
    /// <returns>Task representing the async operation</returns>
    Task SendWorkItemDeletedAsync(string threadId);

    /// <summary>
    /// Sends a work item cancelled notification to the queue for SignalR broadcasting
    /// (marks as cancelled status instead of deleting for tracking)
    /// </summary>
    /// <param name="threadId">The thread/conversation ID</param>
    /// <returns>Task representing the async operation</returns>
    Task SendWorkItemCancelledAsync(string threadId);
}
