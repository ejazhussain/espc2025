// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.AI;

/// <summary>
/// Simple response from Azure AI Agent service for IT support queries
/// </summary>
public class AgentResponse
{
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public double ConfidenceScore { get; set; } = 0.9;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

