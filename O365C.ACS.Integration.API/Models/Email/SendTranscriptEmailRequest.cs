// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace O365C.ACS.Integration.API.Models.Email;

public class SendTranscriptEmailRequest
{
    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("problemReported")]
    public string ProblemReported { get; set; } = string.Empty;

    [JsonPropertyName("solutionProvided")]
    public string SolutionProvided { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("resolutionDate")]
    public string ResolutionDate { get; set; } = string.Empty;
}