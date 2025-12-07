using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace O365C.ACS.SignalR.Hubs;

public class AgentHub
{
    private readonly ILogger<AgentHub> _logger;
    private const string HubName = "agentHub";

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Negotiate function - Returns SignalR connection info
    /// </summary>
    [Function("negotiate")]
    public HttpResponseData Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = HubName, ConnectionStringSetting = "AzureSignalRConnectionString")] string connectionInfo)
    {
        try
        {
            _logger.LogInformation("SignalR negotiate request received");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(connectionInfo);

            _logger.LogInformation("SignalR connection info returned successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in negotiate function");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Queue-triggered function - Broadcasts new chat requests to all connected agents
    /// </summary>
    [Function(nameof(NewChatRequestNotification))]
    [SignalROutput(HubName = HubName, ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction NewChatRequestNotification(
        [QueueTrigger("%NewChatRequestQueue%")] string message)
    {
        _logger.LogWarning("[SignalR] ===== FUNCTION ENTRY POINT HIT =====");
        try
        {
            return Execute(message, "newChatRequest");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SignalR] EXCEPTION IN FUNCTION BODY");
            throw;
        }
    }

    /// <summary>
    /// Queue-triggered function - Broadcasts chat claimed events to all connected agents
    /// </summary>
    [Function(nameof(ChatClaimedNotification))]
    [SignalROutput(HubName = HubName, ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction ChatClaimedNotification(
        [QueueTrigger("%ChatClaimedQueue%")] string message)
    {
        return Execute(message, "chatClaimed");
    }

    /// <summary>
    /// Queue-triggered function - Broadcasts work item cancelled events to all connected agents
    /// </summary>
    [Function(nameof(WorkItemCancelledNotification))]
    [SignalROutput(HubName = HubName, ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction WorkItemCancelledNotification(
        [QueueTrigger("%WorkItemCancelledQueue%")] string message)
    {
        _logger.LogInformation("[SignalR] Processing workItemCancelled message");
        return Execute(message, "workItemCancelled");
    }

    /// <summary>
    /// Generic message processing helper
    /// </summary>
    private SignalRMessageAction Execute(string message, string eventName)
    {
        try
        {
            _logger.LogInformation($"[SignalR] ===== START Processing {eventName} =====");
            _logger.LogInformation($"[SignalR] Message received: {message}");
            _logger.LogInformation($"[SignalR] Message length: {message?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError($"[SignalR] Message is null or empty!");
                throw new ArgumentNullException(nameof(message));
            }

            // Send the raw JSON string directly to preserve the structure
            // SignalR will handle the serialization on the client side
            _logger.LogInformation($"[SignalR] Sending raw message to SignalR clients");

            // Broadcast to all connected clients - send the raw message as a string
            // The SignalR client will parse it correctly
            var action = new SignalRMessageAction(eventName)
            {
                Arguments = new object[] { message }
            };

            _logger.LogInformation($"[SignalR] ===== SUCCESS Processing {eventName} =====");
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SignalR] Unexpected error processing {eventName}. Message: {message}");
            throw; // Re-throw to trigger retry
        }
    }

    /// <summary>
    /// Optional HTTP-triggered function for testing SignalR broadcasts
    /// </summary>
    [Function(nameof(TestBroadcast))]
    [SignalROutput(HubName = HubName, ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction TestBroadcast(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("[SignalR] Test broadcast requested");

            var testMessage = new
            {
                eventType = "test",
                message = "Test broadcast from SignalR Function App",
                timestamp = DateTimeOffset.UtcNow
            };

            return new SignalRMessageAction("testMessage")
            {
                Arguments = new[] { testMessage }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SignalR] Error in test broadcast");

            return new SignalRMessageAction("error")
            {
                Arguments = new[] { new { error = "Test broadcast failed", timestamp = DateTimeOffset.UtcNow } }
            };
        }
    }
}
