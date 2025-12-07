// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.AI;
using O365C.ACS.Integration.API.Models.Settings;
using System.Text;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Azure AI Agent service implementation for IT Support using knowledge base
/// </summary>
public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly ILogger<AzureAIAgentService> _logger;
    private readonly AppSettings _appSettings;
    private readonly PersistentAgentsClient _persistentClient;

    // Cache for the IT support agent
    private PersistentAgent? _itSupportAgent;
    private readonly SemaphoreSlim _agentCreationSemaphore = new(1, 1);

    public AzureAIAgentService(
        ILogger<AzureAIAgentService> logger,
        IOptions<AppSettings> appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));

        try
        {
            // Use DefaultAzureCredential - works for both local dev and Azure deployment
            // Local dev: tries Azure CLI, Visual Studio, Interactive Browser
            // Azure: uses Managed Identity automatically
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = false,   // ENABLE for Azure Function App
                ExcludeVisualStudioCredential = false,      // Enable Visual Studio auth (local)
                ExcludeInteractiveBrowserCredential = false, // Enable interactive browser auth (local fallback)
                ExcludeEnvironmentCredential = false,       // Keep environment variables
                ExcludeAzureCliCredential = false,          // Enable Azure CLI auth (local - preferred)
                ExcludeAzurePowerShellCredential = false,   // Enable Azure PowerShell auth (local)
                ExcludeWorkloadIdentityCredential = false   // Enable for AKS deployments
            });

            // Create Azure AI persistent client
            _persistentClient = new PersistentAgentsClient(
                _appSettings.AzureAIAgent.ConnectionString,
                credential
            );

            _logger.LogInformation("Successfully initialized Azure AI Agent service with agent ID: {AgentId}", _appSettings.AzureAIAgent.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Agent service");
            throw;
        }
    }

    public async Task<AgentResponse> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateErrorResponse("Query cannot be empty");
            }

            _logger.LogInformation("Processing IT support query: {Query}", 
                query.Length > 100 ? query[..100] + "..." : query);

            // Get or create IT support agent
            var agent = await GetOrCreateITSupportAgentAsync(cancellationToken);
            if (agent == null)
            {
                return CreateErrorResponse("Failed to initialize IT support agent");
            }

            // Create thread for the conversation
            var thread = await _persistentClient.Threads.CreateThreadAsync(cancellationToken: cancellationToken);

            // Create message in the thread
            await _persistentClient.Messages.CreateMessageAsync(
                thread.Value.Id,
                MessageRole.User,
                query,
                cancellationToken: cancellationToken
            );

            // Process with streaming
            var responseBuilder = new StringBuilder();
            var streamingUpdates = _persistentClient.Runs.CreateRunStreamingAsync(
                thread.Value.Id,
                agent.Id,
                cancellationToken: cancellationToken
            );

            await foreach (var update in streamingUpdates)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (update.UpdateKind == StreamingUpdateReason.MessageUpdated)
                {
                    if (update is MessageContentUpdate messageContent && !string.IsNullOrEmpty(messageContent.Text))
                    {
                        responseBuilder.Append(messageContent.Text);
                    }
                }
            }

            var response = responseBuilder.ToString();

            // Clean up markdown code blocks if present
            response = CleanMarkdownResponse(response);

            return new AgentResponse
            {
                Response = response,
                Success = true,
                ConfidenceScore = 0.9, // High confidence for knowledge base responses
                Metadata = new Dictionary<string, object>
                {
                    ["ThreadId"] = thread.Value.Id,
                    ["AgentId"] = agent.Id
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing knowledge base query: {Query}", query);
            return CreateErrorResponse(ex.Message);
        }
    }

    /// <summary>
    /// Cleans markdown formatting from AI responses
    /// Removes ```markdown blocks while preserving formatting like **bold**
    /// </summary>
    private string CleanMarkdownResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return response;
        }

        // Remove ```markdown ... ``` blocks but keep the content inside
        response = System.Text.RegularExpressions.Regex.Replace(
            response,
            @"```markdown\s*\n?(.*?)\n?```",
            "$1",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        // Also remove any other code blocks (```language ... ```)
        response = System.Text.RegularExpressions.Regex.Replace(
            response,
            @"```[a-zA-Z]*\s*\n?(.*?)\n?```",
            "$1",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        // Trim any extra whitespace
        response = response.Trim();

        return response;
    }

    private async Task<PersistentAgent?> GetOrCreateITSupportAgentAsync(CancellationToken cancellationToken)
    {
        // Check cache first
        if (_itSupportAgent != null)
        {
            return _itSupportAgent;
        }

        // Use semaphore to prevent multiple requests from loading the same agent
        await _agentCreationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring semaphore
            if (_itSupportAgent != null)
            {
                return _itSupportAgent;
            }

            // Get the pre-configured agent from Azure AI Foundry
            var agent = await _persistentClient.Administration.GetAgentAsync(_appSettings.AzureAIAgent.AgentId, cancellationToken);
            if (agent.HasValue)
            {
                _itSupportAgent = agent.Value;
                _logger.LogInformation("Successfully loaded IT Support agent with ID: {AgentId}", _appSettings.AzureAIAgent.AgentId);
                return _itSupportAgent;
            }
            else
            {
                _logger.LogError("Failed to find IT Support agent with ID: {AgentId}", _appSettings.AzureAIAgent.AgentId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading IT Support agent with ID: {AgentId}. Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                _appSettings.AzureAIAgent.AgentId,
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace);

            // Log inner exception if exists
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception: {InnerExceptionType}, Message: {InnerMessage}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message);
            }

            return null;
        }
        finally
        {
            _agentCreationSemaphore.Release();
        }
    }



    private AgentResponse CreateErrorResponse(string errorMessage)
    {
        return new AgentResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            Response = "I'm having trouble processing your request right now. Please try again or contact IT support directly."
        };
    }
}