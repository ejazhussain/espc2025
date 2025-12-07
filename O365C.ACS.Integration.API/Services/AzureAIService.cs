// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.AI;
using O365C.ACS.Integration.API.Models.Chat;
using O365C.ACS.Integration.API.Models.Settings;

namespace O365C.ACS.Integration.API.Services;

public class AzureAIService : IAzureAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<AzureAIService> _logger;

    public AzureAIService(AppSettings appSettings, ILogger<AzureAIService> logger, IHttpClientFactory httpClientFactory)
    {
        _settings = appSettings.AzureOpenAI ?? throw new ArgumentNullException(nameof(appSettings.AzureOpenAI));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        if (string.IsNullOrWhiteSpace(_settings.Endpoint) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.DeploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI configuration is missing. Ensure Endpoint, ApiKey, and DeploymentName are set.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiVersion))
        {
            _settings.ApiVersion = "2024-05-01-preview";
        }
    }

    public async Task<ChatTranscriptResponse> GenerateTranscriptAsync(GenerateTranscriptRequest request, IReadOnlyList<ChatThreadMessage> messages)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation("[Azure AI] Generating transcript for thread {ThreadId}", request.ThreadId);

        var orderedMessages = messages?.OrderBy(m => m.SentAtUtc).ToList() ?? new List<ChatThreadMessage>();
        var conversationBuilder = new StringBuilder();
        foreach (var message in orderedMessages)
        {
            conversationBuilder.AppendLine($"[{message.SentAtUtc:O}] {message.SenderDisplayName}: {message.Content}");
        }

        var systemPrompt = "You are an assistant that produces concise customer support transcripts. " +
                           "Given the following conversation between a support agent and a customer, " +
                           "produce a strict JSON object with the properties: problemReported (string), " +
                           "solutionProvided (string), summary (string - brief key points and follow-up actions), " +
                           "resolutionDate (string in ISO 8601 or friendly date), and fullTranscript (string). " +
                           "Keep it simple and customer-focused. Do not include any additional commentary or formatting.";

        var userMessageBuilder = new StringBuilder();
        userMessageBuilder.AppendLine($"Thread Id: {request.ThreadId}");
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            userMessageBuilder.AppendLine($"Customer Name: {request.CustomerName}");
        }
        if (!string.IsNullOrWhiteSpace(request.AgentName))
        {
            userMessageBuilder.AppendLine($"Agent Name: {request.AgentName}");
        }
        userMessageBuilder.AppendLine("Conversation:");
        userMessageBuilder.AppendLine(conversationBuilder.ToString());

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessageBuilder.ToString() }
            },
            max_completion_tokens = 10000
        };

        var requestUri = BuildRequestUri();
        using var httpClient = _httpClientFactory.CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("api-key", _settings.ApiKey);
        httpRequest.Headers.Add("Accept", "application/json");

        using var response = await httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("[Azure AI] Transcript generation failed. Status: {Status} Body: {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Azure OpenAI request failed with status code {(int)response.StatusCode}.");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseContent);
        var choicesElement = jsonDocument.RootElement.GetProperty("choices");
        if (choicesElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Azure OpenAI did not return any choices.");
        }

        var firstChoice = choicesElement[0];
        if (!firstChoice.TryGetProperty("message", out var messageElement))
        {
            _logger.LogError("[Azure AI] No 'message' property found in choice. Choice: {Choice}", firstChoice.GetRawText());
            throw new InvalidOperationException("Azure OpenAI response choice did not contain message property.");
        }

        if (!messageElement.TryGetProperty("content", out var contentElement))
        {
            _logger.LogError("[Azure AI] No 'content' property found in message. Message: {Message}", messageElement.GetRawText());
            throw new InvalidOperationException("Azure OpenAI response message did not contain content property.");
        }

        var messageContent = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            // Check if the response was truncated due to token limit
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var finishReasonElement) 
                ? finishReasonElement.GetString() 
                : "unknown";
            
            if (finishReason == "length")
            {
                _logger.LogError("[Azure AI] Response truncated due to token limit. Finish reason: {FinishReason}", finishReason);
                throw new InvalidOperationException("Azure OpenAI response was truncated due to token limit. Consider reducing prompt size or increasing max_completion_tokens.");
            }
            
            _logger.LogError("[Azure AI] Message content is null or empty. Content element: {Content}, Finish reason: {FinishReason}", contentElement.GetRawText(), finishReason);
            throw new InvalidOperationException("Azure OpenAI response did not contain message content.");
        }

        var jsonPayload = messageContent.Trim();
        var startIndex = jsonPayload.IndexOf('{');
        var endIndex = jsonPayload.LastIndexOf('}');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            jsonPayload = jsonPayload[startIndex..(endIndex + 1)];
        }

        ChatTranscriptResponse? transcript;
        try
        {
            transcript = JsonSerializer.Deserialize<ChatTranscriptResponse>(jsonPayload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[Azure AI] Failed to parse transcript JSON. Raw response: {Response}", messageContent);
            throw;
        }

        if (transcript is null)
        {
            throw new InvalidOperationException("Failed to deserialize transcript response.");
        }

        transcript.ThreadId = request.ThreadId;
        transcript.CustomerName = request.CustomerName ?? transcript.CustomerName;
        transcript.AgentName = request.AgentName ?? transcript.AgentName;
        transcript.FullTranscript = string.IsNullOrWhiteSpace(transcript.FullTranscript)
            ? conversationBuilder.ToString()
            : transcript.FullTranscript;
        transcript.ResolutionDate = string.IsNullOrWhiteSpace(transcript.ResolutionDate)
            ? DateTimeOffset.UtcNow.ToString("d")
            : transcript.ResolutionDate;

        transcript.Summary = string.IsNullOrWhiteSpace(transcript.Summary)
            ? "No additional follow-up actions required."
            : transcript.Summary;

        _logger.LogInformation("[Azure AI] Generated transcript for thread {ThreadId}", request.ThreadId);

        return transcript;
    }

    private Uri BuildRequestUri()
    {
        var builder = new UriBuilder(_settings.Endpoint)
        {
            Path = $"openai/deployments/{_settings.DeploymentName}/chat/completions",
            Query = $"api-version={_settings.ApiVersion}"
        };

        return builder.Uri;
    }
}
