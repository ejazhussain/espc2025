// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Meeting;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Service interface for Teams meeting management operations
/// Handles creation, retrieval, and cancellation of Teams meetings via Microsoft Graph
/// </summary>
public interface ITeamsMeetingService
{
    /// <summary>
    /// Creates a new Teams meeting and returns meeting details
    /// </summary>
    /// <param name="request">Meeting creation request with attendee and schedule information</param>
    /// <returns>Response containing meeting join URL and event details</returns>
    Task<CreateMeetingResponse> CreateTeamsMeetingAsync(CreateMeetingRequest request);

    /// <summary>
    /// Retrieves details of an existing Teams meeting
    /// </summary>
    /// <param name="eventId">Calendar event ID of the meeting</param>
    /// <param name="organizerEmail">Email of the meeting organizer</param>
    /// <returns>Detailed meeting information including join URL and attendees</returns>
    Task<MeetingDetails?> GetMeetingDetailsAsync(string eventId, string organizerEmail);

    /// <summary>
    /// Cancels a Teams meeting
    /// </summary>
    /// <param name="eventId">Calendar event ID of the meeting to cancel</param>
    /// <param name="organizerEmail">Email of the meeting organizer</param>
    /// <param name="cancellationMessage">Optional message to send to attendees</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelMeetingAsync(string eventId, string organizerEmail, string? cancellationMessage = null);

    /// <summary>
    /// Updates an existing Teams meeting
    /// </summary>
    /// <param name="eventId">Calendar event ID of the meeting to update</param>
    /// <param name="organizerEmail">Email of the meeting organizer</param>
    /// <param name="request">Updated meeting information</param>
    /// <returns>Response with updated meeting details</returns>
    Task<CreateMeetingResponse> UpdateMeetingAsync(string eventId, string organizerEmail, CreateMeetingRequest request);
}
