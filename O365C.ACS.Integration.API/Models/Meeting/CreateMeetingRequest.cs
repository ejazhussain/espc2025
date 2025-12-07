// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace O365C.ACS.Integration.API.Models.Meeting;

/// <summary>
/// Request model for creating a Teams meeting
/// </summary>
public class CreateMeetingRequest
{
    /// <summary>
    /// ACS chat thread ID to associate meeting with
    /// </summary>
    [Required]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Customer's display name
    /// </summary>
    [Required]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address (optional for calendar invite)
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Agent's email address who will host the meeting
    /// </summary>
    [Required]
    [EmailAddress]
    public string AgentEmail { get; set; } = string.Empty;

    /// <summary>
    /// Meeting start date and time in UTC
    /// </summary>
    [Required]
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Meeting end date and time in UTC
    /// </summary>
    [Required]
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Meeting subject/title
    /// </summary>
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Optional meeting description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Time zone for the meeting (default: UTC)
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
}
