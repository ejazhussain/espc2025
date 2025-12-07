// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Request model for sending a message to an ACS chat thread
/// Contains all necessary information to send a message via the API
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// ACS User ID of the message sender
    /// </summary>
    [Required]
    public required string UserId { get; set; }

    /// <summary>
    /// Display name of the message sender
    /// </summary>
    [Required]
    public required string DisplayName { get; set; }

    /// <summary>
    /// ACS Thread ID where the message will be sent
    /// </summary>
    [Required]
    public required string ThreadId { get; set; }

    /// <summary>
    /// Message content to be sent
    /// </summary>
    [Required]
    public required string Message { get; set; }

    /// <summary>
    /// Message type (text, html, etc.)
    /// Defaults to "text" if not specified
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Optional metadata to attach to the message
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Timestamp when the message was created on the client side
    /// </summary>
    public DateTimeOffset? ClientTimestamp { get; set; }
}

/// <summary>
/// Response model for message sending operations
/// Contains information about the sent message and operation status
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// Indicates if the message was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ACS Message ID assigned by the service
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Thread ID where the message was sent
    /// </summary>
    public required string ThreadId { get; set; }

    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// Display name of the message sender
    /// </summary>
    public required string SenderDisplayName { get; set; }

    /// <summary>
    /// Content of the sent message
    /// </summary>
    public required string MessageContent { get; set; }

    /// <summary>
    /// Type of the message (text, html, etc.)
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if the operation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Additional metadata about the message or operation
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}