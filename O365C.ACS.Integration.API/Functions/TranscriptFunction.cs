// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.AI;

namespace O365C.ACS.Integration.API;

public class TranscriptFunction
{
    private readonly ILogger<TranscriptFunction> _logger;
    private readonly IAzureAIService _azureAIService;
    private readonly IChatService _chatService;

    public TranscriptFunction(ILogger<TranscriptFunction> logger, IAzureAIService azureAIService, IChatService chatService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureAIService = azureAIService ?? throw new ArgumentNullException(nameof(azureAIService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
    }

    [Function("GenerateTranscript")]
    public async Task<IActionResult> GenerateTranscriptAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/transcript")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<GenerateTranscriptRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (request is null || string.IsNullOrWhiteSpace(request.ThreadId))
            {
                return new BadRequestObjectResult(new { error = "threadId is required" });
            }

            _logger.LogInformation("[AI Function] Generating transcript for thread {ThreadId}", request.ThreadId);

            var messages = await _chatService.GetThreadMessagesAsync(request.ThreadId);
            if (messages.Count == 0)
            {
                _logger.LogWarning("[AI Function] No messages found for thread {ThreadId}", request.ThreadId);
            }

            var transcript = await _azureAIService.GenerateTranscriptAsync(request, messages);

            return new OkObjectResult(transcript);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[AI Function] Invalid JSON payload for transcript generation");
            return new BadRequestObjectResult(new { error = "Invalid request format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI Function] Unexpected error while generating transcript");
            return new ObjectResult(new { error = "Failed to generate transcript" }) { StatusCode = 500 };
        }
    }
}
