using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Agent;
using O365C.ACS.Integration.API.Models.Chat;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Helpers;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Services
{
    /// <summary>
    /// Teams Notification Service for sending activity notifications to Microsoft Teams users.
    /// 
    /// IMPORTANT ARCHITECTURE NOTE - Teams Authentication Requirements:
    /// ================================================================
    /// 
    /// This service requires a SEPARATE Azure AD application and Graph client specifically 
    /// configured for Teams operations. Here's why and how:
    /// 
    /// 1. DUAL AZURE AD APP ARCHITECTURE:
    ///    - Teams App (824b456d-f349-494a-a2ee-af364177ce80): Teams app authentication
    ///    - Backend App (ae62e16f-fa05-497b-9253-3bd8a96df867): General API operations
    /// 
    /// 2. WHY SEPARATE CLIENTS ARE NEEDED:
    ///    - Teams notifications require specific app permissions and manifest configuration
    ///    - The Teams app must be installed in the target Teams environment
    ///    - Different permission scopes are required (TeamsActivity.Send vs general Graph API)
    ///    - Authentication context differs between user-facing Teams app and backend API
    /// 
    /// 3. GRAPH CLIENT FACTORY PATTERN:
    ///    - Uses IGraphClientFactory to create authenticated GraphServiceClient instances
    ///    - GraphClientFactory.CreateTeamsGraphClient() uses Teams app credentials (frontend app)
    ///    - This ensures proper authentication context for Teams activity notifications
    /// 
    /// 4. TEAMS APP MANIFEST REQUIREMENTS:
    ///    - Teams app (6a47d6eb-43a9-4036-838c-245f8be13204) must include activity types
    ///    - systemDefault activity type must be configured in manifest.json
    ///    - App must be installed in target Teams environment
    /// 
    /// 5. REQUIRED PERMISSIONS:
    ///    - TeamsActivity.Send: Send activity notifications
    ///    - User.Read.All: Read user information
    ///    - Team.ReadBasic.All: Read basic team information
    /// 
    /// 6. TROUBLESHOOTING TIPS:
    ///    - Verify Teams app is installed and has correct permissions
    ///    - Check that frontend app credentials are configured in TeamsApp settings
    ///    - Ensure user-specific notification endpoint is used vs team-based
    ///    - Use systemDefault activity type with systemDefaultText parameter
    /// 
    /// 7. DEEP LINK CONFIGURATION:
    ///    CRITICAL: WebUrl format determines click behavior:
    ///    ❌ WRONG: /l/app/{appId} → Shows app info dialog
    ///    ✅ CORRECT: /l/entity/{appId}/index?context={"subEntityId":"{threadId}"} → Opens app directly
    ///    
    ///    The entity format allows:
    ///    - Direct app navigation without installation dialog
    ///    - Passing context (like threadId) to the app
    ///    - Better user experience with immediate app access
    /// 
    /// 8. TESTING NOTIFICATIONS:
    ///    - Use Postman with proper Bearer token for initial testing
    ///    - Verify user receives notification in Teams Activity Feed
    ///    - Click notification should open Teams app, not show app info
    ///    - Check browser console for any JavaScript errors in Teams app
    /// 
    /// For more details, see: TEAMS_NOTIFICATION_SETUP.md
    /// </summary>
    public class TeamsNotificationService : ITeamsNotificationService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<TeamsNotificationService> _logger;
        private readonly AppSettings _appSettings;

        /// <summary>
        /// Initializes the Teams Notification Service with a dedicated Teams Graph client.
        /// Uses IGraphClientFactory to create a GraphServiceClient authenticated with Teams app credentials.
        /// </summary>
        public TeamsNotificationService(
            ILogger<TeamsNotificationService> logger,
            IOptions<AppSettings> appSettings,
            IGraphClientFactory graphClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));

            // CRITICAL: Use the Teams Graph client for all operations
            // This client is authenticated with the frontend Azure AD app credentials
            // which has the necessary Teams permissions and app installation
            _graphServiceClient = graphClientFactory?.CreateTeamsGraphClient() ?? throw new ArgumentNullException(nameof(graphClientFactory));

            _logger.LogInformation("TeamsNotificationService initialized with Teams Graph client using app {AppId}", 
                _appSettings.TeamsApp.AppId);
        }

        public async Task<bool> SendActivityNotificationToAgentAsync(string agentUserId, CreateThreadResponse chatRequest, string priority = "NORMAL", string? customerName = null, DateTime? requestTime = null, string? questionSummary = null, string? chatTopic = null, string? initialMessage = null)
        {
            try
            {
                // Check if Teams notifications are enabled
                if (!_appSettings.TeamsApp.EnableNotifications)
                {
                    _logger.LogInformation("Teams notifications are disabled via configuration");
                    return true;
                }

                _logger.LogInformation("Sending Teams activity notification to agent {AgentId} for chat {ThreadId}", 
                    agentUserId, chatRequest.ThreadId);

                // CRITICAL: Use proper deep link format to open the app directly (not show app info dialog)
                // Format: /l/entity/{appId}/{tabName}?context={"subEntityId":"{contentId}"}
                // This ensures clicking the notification opens the Teams app rather than the app info dialog
                var webUrl = $"https://teams.microsoft.com/l/entity/{_appSettings.TeamsApp.AppId}/index?context={{\"subEntityId\":\"{chatRequest.ThreadId}\"}}";

                // Create clear, actionable notification messages based on priority and context
                var isHighPriority = priority.ToUpperInvariant() == "HIGH" || priority.ToUpperInvariant() == "URGENT";
                
                // Format customer name and timestamp for display
                var displayName = !string.IsNullOrEmpty(customerName) ? customerName : "Customer";
                var timeStamp = FormatNotificationTime(requestTime ?? DateTime.UtcNow);
                
                // Extract meaningful context for the notification
                var contextInfo = ExtractNotificationContext(questionSummary, chatTopic, initialMessage);
                
                // Use text source for simplicity and reliability (as tested in Postman)
                // Topic appears at the bottom of the notification and should include date/time
                var topicValue = !string.IsNullOrEmpty(chatTopic) 
                    ? $"{displayName} > {chatTopic} ({timeStamp})"
                    : !string.IsNullOrEmpty(customerName) 
                        ? $"{displayName} > Support Request ({timeStamp})" 
                        : $"Customer Support ({timeStamp})";
                        
                var notification = new TeamworkActivityTopic
                {
                    Source = TeamworkActivityTopicSource.Text,
                    Value = topicValue,
                    WebUrl = webUrl
                };

                var activityType = "systemDefault"; // Use systemDefault for maximum compatibility
                
                // Enhanced preview text with context
                var previewContent = GenerateContextualPreview(displayName, contextInfo, isHighPriority);
                var previewText = new ItemBody
                {
                    Content = previewContent
                };

                // Enhanced systemDefaultText with more context
                var systemText = GenerateContextualSystemText(displayName, contextInfo, isHighPriority);
                var templateParameters = new List<Microsoft.Graph.Models.KeyValuePair>
                {
                    new Microsoft.Graph.Models.KeyValuePair
                    {
                        Name = "systemDefaultText",
                        Value = systemText
                    }
                };

                // ⚠️ CRITICAL CONFIGURATION: TeamsAppId must be the INTERNAL App ID
                // ========================================================================
                //
                // How to find the correct TeamsAppId:
                // 1. Call Graph API: GET https://graph.microsoft.com/v1.0/users/{userId}/teamwork/installedApps?$expand=teamsApp
                // 2. Find your Teams app in the response array
                // 3. Use the "id" field from "teamsApp" object (NOT "externalId")
                //
                // Example API Response:
                // {
                //   "value": [
                //     {
                //       "id": "MmE1ZGUzNDYtMWQ2My00YzdhLTg5N2YtYjFmNGI1MzE2ZmU1IyM3NWM5ZmE4Ny1jNWM1LTQ0NWQtOWIyYi1hZWY3MjFiY2EzZGY=",
                //       "teamsApp": {
                //         "id": "75c9fa87-c5c5-445d-9b2b-aef721bca3df",              // ✅ USE THIS (internal/installed app ID)
                //         "externalId": "ab64c4ea-56f3-459d-b9ee-4f2bdfa195e1",     // ❌ DON'T USE (external/manifest ID)
                //         "displayName": "[O365C] Support Agents",
                //         "distributionMethod": "sideloaded"
                //       }
                //     }
                //   ]
                // }
                //
                // Why this matters:
                // - Teams activity notifications REQUIRE the installed app ID (internal ID shown in "id" field)
                // - Using the external ID (from manifest.json) will cause notifications to FAIL SILENTLY
                // - The internal ID is generated when the app is installed in Teams
                // - The external ID is the app manifest ID and is NOT recognized by the notification API
                // - This was discovered after extensive testing - notifications only work with internal ID
                //
                // Configuration location: local.settings.json → TeamsApp:AppId
                var sendActivityNotificationPostRequestBody = new Microsoft.Graph.Users.Item.Teamwork.SendActivityNotification.SendActivityNotificationPostRequestBody
                {
                    Topic = notification,
                    ActivityType = activityType,
                    PreviewText = previewText,
                    TemplateParameters = templateParameters,
                    TeamsAppId = _appSettings.TeamsApp.AppId  // Must be internal ID from installedApps API
                };

                        // Diagnostic logging for troubleshooting notification issues
                        _logger.LogInformation("SendActivityNotification: agentUserId={AgentUserId}, TeamsAppId={TeamsAppId}, webUrl={WebUrl}", 
                            agentUserId, _appSettings.TeamsApp.AppId, $"https://teams.microsoft.com/l/entity/{_appSettings.TeamsApp.AppId}/index?context={{\"subEntityId\":\"{chatRequest.ThreadId}\"}}" );
                // Use the Teams Graph client with proper frontend app authentication
                await _graphServiceClient.Users[agentUserId].Teamwork.SendActivityNotification
                    .PostAsync(sendActivityNotificationPostRequestBody);

                _logger.LogInformation("Successfully sent Teams activity notification to agent {AgentId}", agentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Teams activity notification to agent {AgentId} for chat {ThreadId}", 
                    agentUserId, chatRequest.ThreadId);
                return false;
            }
        }

        /// <summary>
        /// Broadcasts a new chat notification to all configured agents
        /// </summary>
        public async Task<int> BroadcastNewChatNotificationAsync(
            CreateThreadResponse chatRequest, 
            string customerName, 
            string priority = "NORMAL", 
            string? initialMessage = null)
        {
            try
            {
                // Check if Teams notifications are enabled
                if (!_appSettings.TeamsApp.EnableNotifications)
                {
                    _logger.LogInformation("Teams notifications are disabled via configuration");
                    return 0;
                }

                _logger.LogInformation("Broadcasting Teams notification to all agents for chat {ThreadId}", chatRequest.ThreadId);

                // Get all configured agents
                var agents = _appSettings.AgentUsers?.Users;
                if (agents == null || !agents.Any())
                {
                    _logger.LogWarning("No agents configured for Teams notifications");
                    return 0;
                }

                int successCount = 0;
                var tasks = new List<Task<bool>>();

                // Send notification to each agent
                foreach (var agent in agents)
                {
                    if (!string.IsNullOrEmpty(agent.TeamsUserId))
                    {
                        _logger.LogDebug("Sending Teams notification to agent {AgentName} ({AgentId})", 
                            agent.DisplayName, agent.TeamsUserId);

                        var task = SendActivityNotificationToAgentAsync(
                            agent.TeamsUserId,
                            chatRequest,
                            priority,
                            customerName,
                            DateTime.UtcNow,
                            null, // questionSummary
                            null, // chatTopic
                            initialMessage
                        );
                        tasks.Add(task);
                    }
                }

                // Wait for all notifications to complete
                var results = await Task.WhenAll(tasks);
                successCount = results.Count(r => r);

                _logger.LogInformation("Broadcast complete: {SuccessCount}/{TotalCount} agents notified for chat {ThreadId}",
                    successCount, agents.Count, chatRequest.ThreadId);

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast Teams notification for chat {ThreadId}", chatRequest.ThreadId);
                return 0;
            }
        }

        /// <summary>
        /// Sends a new chat notification to the current logged-in agent
        /// This targets the agent who claimed/is viewing the support queue
        /// </summary>
        public async Task<bool> SendNewChatNotificationToAgentAsync(string agentUserId, CreateThreadResponse chatRequest, string customerName, string priority = "NORMAL", string? initialMessage = null)
        {
            try
            {
                _logger.LogInformation("Sending Teams notification to agent {AgentId} for new chat {ThreadId}",
                    agentUserId, chatRequest.ThreadId);

                return await SendActivityNotificationToAgentAsync(
                    agentUserId,
                    chatRequest,
                    priority,
                    customerName,
                    DateTime.UtcNow,
                    null, // questionSummary
                    null, // chatTopic
                    initialMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Teams notification to agent {AgentId} for chat {ThreadId}",
                    agentUserId, chatRequest.ThreadId);
                return false;
            }
        }

        /// <summary>
        /// Formats timestamp for Teams notifications in a user-friendly way
        /// Examples: "Today 22:24", "Yesterday 14:30", "Dec 15 09:15"
        /// </summary>
        private static string FormatNotificationTime(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var requestDate = dateTime.Date;
            
            var timeString = dateTime.ToString("HH:mm");
            
            if (requestDate == today)
            {
                return $"Today {timeString}";
            }
            else if (requestDate == today.AddDays(-1))
            {
                return $"Yesterday {timeString}";
            }
            else
            {
                var monthDay = dateTime.ToString("MMM dd");
                return $"{monthDay} {timeString}";
            }
        }

        private async Task<string?> GetDefaultChannelIdAsync(string teamId)
        {
            try
            {
                _logger.LogDebug("Getting default channel for team {TeamId}", teamId);
                
                // Get all channels from the team using the Graph client
                var channels = await _graphServiceClient.Teams[teamId].Channels
                    .GetAsync();

                if (channels?.Value == null || !channels.Value.Any())
                {
                    _logger.LogWarning("No channels found for team {TeamId}", teamId);
                    return null;
                }

                // Find the "General" channel (default channel)
                var generalChannel = channels.Value.FirstOrDefault(c => 
                    string.Equals(c.DisplayName, "General", StringComparison.OrdinalIgnoreCase));

                if (generalChannel != null)
                {
                    _logger.LogDebug("Found General channel: {ChannelId}", generalChannel.Id);
                    return generalChannel.Id;
                }

                // If no "General" channel found, use the first channel
                var firstChannel = channels.Value.FirstOrDefault();
                if (firstChannel != null)
                {
                    _logger.LogDebug("Using first available channel: {ChannelId}", firstChannel.Id);
                    return firstChannel.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default channel for team {TeamId}", teamId);
                return null;
            }
        }

        /// <summary>
        /// Extracts relevant context information from customer input for notification display
        /// </summary>
        private static string ExtractNotificationContext(string? questionSummary, string? chatTopic, string? initialMessage)
        {
            // Priority order: questionSummary, chatTopic, initialMessage
            if (!string.IsNullOrWhiteSpace(questionSummary))
            {
                return TruncateForNotification(questionSummary, 80);
            }
            
            if (!string.IsNullOrWhiteSpace(chatTopic))
            {
                return TruncateForNotification(chatTopic, 80);
            }
            
            if (!string.IsNullOrWhiteSpace(initialMessage))
            {
                return TruncateForNotification(initialMessage, 80);
            }
            
            return "General support assistance";
        }

        /// <summary>
        /// Generates contextual preview text for the notification
        /// </summary>
        private static string GenerateContextualPreview(string displayName, string contextInfo, bool isHighPriority)
        {
            if (isHighPriority)
            {
                return $"🚨 Urgent support request";
            }
            
            return $"💬 New support request";
        }

        /// <summary>
        /// Generates contextual system text for the notification main content
        /// </summary>
        private static string GenerateContextualSystemText(string displayName, string contextInfo, bool isHighPriority)
        {
            // This is the main content line - show the actual issue/question
            return contextInfo;
        }

        /// <summary>
        /// Truncates text to fit notification limits while preserving readability
        /// </summary>
        private static string TruncateForNotification(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            var trimmed = text.Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;
                
            // Find a good break point (space) near the limit
            var truncated = trimmed.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOf(' ');
            
            if (lastSpace > maxLength * 0.7) // If we find a space in the last 30% of text
            {
                return trimmed.Substring(0, lastSpace) + "...";
            }
            
            return truncated + "...";
        }
    }
}
