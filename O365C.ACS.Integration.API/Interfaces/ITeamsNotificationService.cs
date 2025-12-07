using O365C.ACS.Integration.API.Models.Chat;

namespace O365C.ACS.Integration.API.Interfaces
{
    public interface ITeamsNotificationService
    {
        /// <summary>
        /// Sends a Teams activity notification to a specific agent using systemDefault activity type
        /// This doesn't require activity types declared in the manifest
        /// </summary>
        /// <param name="agentUserId">Azure AD User ID of the agent</param>
        /// <param name="chatRequest">Chat request details</param>
        /// <param name="priority">Priority level (NORMAL, HIGH, CRITICAL)</param>
        /// <param name="customerName">Optional customer name for personalized notifications</param>
        /// <param name="requestTime">Optional request timestamp for context</param>
        /// <param name="questionSummary">Optional customer's question summary for context</param>
        /// <param name="chatTopic">Optional chat topic extracted from customer question</param>
        /// <param name="initialMessage">Optional first message content for preview</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendActivityNotificationToAgentAsync(string agentUserId, CreateThreadResponse chatRequest, string priority = "NORMAL", string? customerName = null, DateTime? requestTime = null, string? questionSummary = null, string? chatTopic = null, string? initialMessage = null);
      
        /// <summary>
        /// Broadcasts a new chat notification to all configured agents
        /// </summary>
        /// <param name="chatRequest">Chat request details</param>
        /// <param name="customerName">Customer name for the notification</param>
        /// <param name="priority">Priority level (NORMAL, HIGH, CRITICAL)</param>
        /// <param name="initialMessage">Optional first message content</param>
        /// <returns>Number of agents successfully notified</returns>
        Task<int> BroadcastNewChatNotificationAsync(CreateThreadResponse chatRequest, string customerName, string priority = "NORMAL", string? initialMessage = null);

        /// <summary>
        /// Sends a new chat notification to the current logged-in agent
        /// This targets the agent who claimed/is viewing the support queue
        /// </summary>
        /// <param name="agentUserId">Azure AD User ID of the current agent</param>
        /// <param name="chatRequest">Chat request details</param>
        /// <param name="customerName">Customer name for the notification</param>
        /// <param name="priority">Priority level (NORMAL, HIGH, CRITICAL)</param>
        /// <param name="initialMessage">Optional first message content</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendNewChatNotificationToAgentAsync(string agentUserId, CreateThreadResponse chatRequest, string customerName, string priority = "NORMAL", string? initialMessage = null);
    }
}
