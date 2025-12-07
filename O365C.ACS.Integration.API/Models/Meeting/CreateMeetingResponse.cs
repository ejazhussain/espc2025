// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Meeting;

/// <summary>
/// Response model for meeting creation
/// </summary>
public class CreateMeetingResponse
{
    /// <summary>
    /// Indicates if meeting creation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Teams calendar event ID
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Teams meeting join URL for attendees
    /// </summary>
    public string? JoinUrl { get; set; }

    /// <summary>
    /// Conference ID for dial-in
    /// </summary>
    public string? ConferenceId { get; set; }

    /// <summary>
    /// Meeting start time
    /// </summary>
    public DateTime? StartDateTime { get; set; }

    /// <summary>
    /// Meeting end time
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    /// <summary>
    /// Associated chat thread ID
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
