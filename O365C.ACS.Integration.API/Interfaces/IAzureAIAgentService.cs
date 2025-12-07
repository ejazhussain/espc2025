// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.AI;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for Azure AI Agent service that provides IT support
/// using Azure AI Persistent Agents with knowledge base integration
/// </summary>
public interface IAzureAIAgentService
{
    /// <summary>
    /// Processes customer query using IT Support Knowledge Base agent
    /// </summary>
    /// <param name="query">Customer query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI agent response with IT support guidance</returns>
    Task<AgentResponse> ProcessQueryAsync(string query, CancellationToken cancellationToken = default);
}