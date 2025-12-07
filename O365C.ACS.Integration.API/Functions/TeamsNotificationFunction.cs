using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Helpers;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Chat;
using System.Net;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Functions
{
    public class TeamsNotificationFunction
    {
        private readonly ILogger<TeamsNotificationFunction> _logger;
        private readonly ITeamsNotificationService _teamsNotificationService;

        public TeamsNotificationFunction(
            ILogger<TeamsNotificationFunction> logger,
            ITeamsNotificationService teamsNotificationService)
        {
            _logger = logger;
            _teamsNotificationService = teamsNotificationService;
        }

        private static void AddCorsHeaders(HttpResponseData response, HttpRequestData request)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Accept, Origin, User-Agent");
        }

        [Function("NotifyAgentsTeams")]
        public async Task<HttpResponseData> NotifyAgentsTeams(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "teams/notify")] HttpRequestData req)
        {
            _logger.LogInformation("Teams notification request received");

            try
            {
                // Add CORS headers
                var response = req.CreateResponse();
                AddCorsHeaders(response, req);

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Request body is required");
                    return response;
                }

                var notificationRequest = JsonSerializer.Deserialize<TeamsNotificationRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (notificationRequest == null || notificationRequest.ChatRequest == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid notification request");
                    return response;
                }

                bool notificationSent;

                // Send notification to specific agent only - requires agentUserId
                if (!string.IsNullOrEmpty(notificationRequest.AgentUserId))
                {
                    _logger.LogInformation("Sending Teams notification to specific agent: {AgentId}", notificationRequest.AgentUserId);
                    notificationSent = await _teamsNotificationService.SendActivityNotificationToAgentAsync(
                        notificationRequest.AgentUserId,
                        notificationRequest.ChatRequest,
                        notificationRequest.Priority ?? "NORMAL",
                        notificationRequest.CustomerName,
                        notificationRequest.RequestTime,
                        notificationRequest.QuestionSummary,
                        notificationRequest.ChatTopic,
                        notificationRequest.InitialMessage);
                }
                else
                {
                    _logger.LogWarning("No specific agent ID provided - notification cannot be sent");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    var errorResponse = new
                    {
                        success = false,
                        message = "AgentUserId is required for Teams notifications",
                        threadId = notificationRequest.ChatRequest.ThreadId
                    };
                    await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
                    return response;
                }

                if (notificationSent)
                {
                    response.StatusCode = HttpStatusCode.OK;
                    var successResponse = new
                    {
                        success = true,
                        message = "Teams notification sent successfully",
                        threadId = notificationRequest.ChatRequest.ThreadId,
                        timestamp = DateTime.UtcNow
                    };
                    await response.WriteStringAsync(JsonSerializer.Serialize(successResponse));
                }
                else
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    var errorResponse = new
                    {
                        success = false,
                        message = "Failed to send Teams notification",
                        threadId = notificationRequest.ChatRequest.ThreadId
                    };
                    await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Teams notification request");
                
                var errorResponse = req.CreateResponse();
                AddCorsHeaders(errorResponse, req);
                errorResponse.StatusCode = HttpStatusCode.InternalServerError;
                await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
                return errorResponse;
            }
        }       
    }
    public class TeamsNotificationRequest
    {
        public CreateThreadResponse ChatRequest { get; set; } = new();
        public string? AgentUserId { get; set; }
        public string? Priority { get; set; } = "NORMAL";
        public string? CustomerName { get; set; }
        public DateTime? RequestTime { get; set; }
        public string? QuestionSummary { get; set; }
        public string? ChatTopic { get; set; }
        public string? InitialMessage { get; set; }
    }
}
