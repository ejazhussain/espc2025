// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Meeting;

/// <summary>
/// Detailed information about a Teams meeting
/// </summary>
public class MeetingDetails
{
    /// <summary>
    /// Calendar event ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Meeting subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Meeting description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Meeting start time
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Meeting end time
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Teams meeting join URL
    /// </summary>
    public string JoinUrl { get; set; } = string.Empty;

    /// <summary>
    /// Conference ID for dial-in
    /// </summary>
    public string? ConferenceId { get; set; }

    /// <summary>
    /// Dial-in phone number
    /// </summary>
    public string? TollNumber { get; set; }

    /// <summary>
    /// Organizer email
    /// </summary>
    public string OrganizerEmail { get; set; } = string.Empty;

    /// <summary>
    /// List of attendees
    /// </summary>
    public List<MeetingAttendee> Attendees { get; set; } = new();

    /// <summary>
    /// Associated chat thread ID
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Meeting status
    /// </summary>
    public string Status { get; set; } = "Scheduled";
}

/// <summary>
/// Information about a meeting attendee
/// </summary>
public class MeetingAttendee
{
    /// <summary>
    /// Attendee name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Attendee email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Attendee type (Required, Optional, etc.)
    /// </summary>
    public string Type { get; set; } = "Required";

    /// <summary>
    /// Response status
    /// </summary>
    public string? ResponseStatus { get; set; }
}
