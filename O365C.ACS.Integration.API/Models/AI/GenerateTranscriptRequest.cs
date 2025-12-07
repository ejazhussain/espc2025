// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace O365C.ACS.Integration.API.Models.AI;

public class GenerateTranscriptRequest
{
    [Required]
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }
}
