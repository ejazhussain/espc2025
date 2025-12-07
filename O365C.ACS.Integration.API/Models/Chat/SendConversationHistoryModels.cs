// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Request model for sending conversation history from AI chat to ACS chat thread
/// Contains conversation messages to be sent to provide context to agents
/// </summary>
public class SendConversationHistoryRequest
{
    /// <summary>
    /// ACS Thread ID where the conversation history will be sent
    /// </summary>
    [Required]
    public required string ThreadId { get; set; }

    /// <summary>
    /// List of conversation messages from AI chat
    /// </summary>
    [Required]
    public required List<ConversationHistoryMessage> ConversationHistory { get; set; }
}

/// <summary>
/// Individual message from conversation history
/// </summary>
public class ConversationHistoryMessage
{
    /// <summary>
    /// Message content
    /// </summary>
    [Required]
    public required string Content { get; set; }

    /// <summary>
    /// Display name of the message sender (Customer, AI Assistant, System)
    /// </summary>
    [Required]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    [Required]
    public required string Timestamp { get; set; }

    /// <summary>
    /// Sender display name for ACS
    /// </summary>
    [Required]
    public required string SenderDisplayName { get; set; }

    /// <summary>
    /// Message type (always "text" for conversation history)
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// Message type for ACS (always "text" for conversation history)
    /// </summary>
    public string MessageType { get; set; } = "text";
}

/// <summary>
/// Response model for sending conversation history to ACS chat thread
/// Contains the result of the conversation history send operation
/// </summary>
public class SendConversationHistoryResponse
{
    /// <summary>
    /// Indicates if the conversation history was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ACS Thread ID where the conversation history was sent
    /// </summary>
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Number of messages successfully sent
    /// </summary>
    public int MessagesSent { get; set; }

    /// <summary>
    /// Total number of messages attempted to send
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp when the operation was completed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}