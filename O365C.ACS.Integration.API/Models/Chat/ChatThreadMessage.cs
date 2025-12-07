// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Simplified representation of a chat message used for AI transcript generation.
/// </summary>
public class ChatThreadMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("senderId")]
    public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("senderDisplayName")]
    public string SenderDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("sentAtUtc")]
    public DateTimeOffset SentAtUtc { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
