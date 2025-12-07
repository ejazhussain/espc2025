// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.AI;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Functions;

/// <summary>
/// Azure Function for AI Agent operations using Azure AI Persistent Agents
/// </summary>
public class AzureAIAgentFunction
{
    private readonly ILogger<AzureAIAgentFunction> _logger;
    private readonly IAzureAIAgentService _azureAIAgentService;

    public AzureAIAgentFunction(
        ILogger<AzureAIAgentFunction> logger,
        IAzureAIAgentService azureAIAgentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureAIAgentService = azureAIAgentService ?? throw new ArgumentNullException(nameof(azureAIAgentService));
    }

    [Function("ProcessAgentQuery")]
    public async Task<IActionResult> ProcessAgentQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent/query")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Processing agent query request");

            // Read request body
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            // Parse request
            var request = JsonSerializer.Deserialize<AgentQueryRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new BadRequestObjectResult(new { error = "Invalid request: query is required" });
            }

            // Process query with Knowledge Base agent
            var response = await _azureAIAgentService.ProcessQueryAsync(request.Query);

            if (!response.Success)
            {
                _logger.LogError("Agent processing failed: {ErrorMessage}", response.ErrorMessage);
                return new StatusCodeResult(500);
            }

            _logger.LogInformation("Successfully processed agent query, confidence: {Confidence}",
                response.ConfidenceScore);

            return new OkObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing IT Support query");
            return new StatusCodeResult(500);
        }
    }



   
}

/// <summary>
/// Request model for agent query processing
/// </summary>
public class AgentQueryRequest
{
    public string Query { get; set; } = string.Empty;
}