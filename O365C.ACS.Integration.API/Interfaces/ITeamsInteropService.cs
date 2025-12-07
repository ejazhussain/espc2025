// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Meeting;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Service interface for Teams interoperability operations
/// Handles ACS token generation and meeting join functionality for external users (BYOI)
/// </summary>
public interface ITeamsInteropService
{
    /// <summary>
    /// Generates an ACS access token for a customer to join a Teams meeting
    /// Implements BYOI (Bring Your Own Identity) model for external users
    /// </summary>
    /// <param name="request">Token request with user information</param>
    /// <returns>Response containing ACS token and user ID</returns>
    Task<InteropTokenResponse> GetMeetingAccessTokenAsync(InteropTokenRequest request);

    /// <summary>
    /// Parses a Teams meeting link to extract meeting coordinates
    /// </summary>
    /// <param name="meetingLink">Teams meeting URL</param>
    /// <param name="threadId">Optional thread ID to associate</param>
    /// <returns>Meeting join information</returns>
    Task<MeetingJoinInfo> ParseTeamsMeetingLinkAsync(string meetingLink, string? threadId = null);

    /// <summary>
    /// Validates if a customer can access a specific meeting
    /// Checks if customer is part of the associated chat thread
    /// </summary>
    /// <param name="request">Validation request with thread and user info</param>
    /// <returns>Validation response indicating access permission</returns>
    Task<ValidateMeetingAccessResponse> ValidateMeetingAccessAsync(ValidateMeetingAccessRequest request);

    /// <summary>
    /// Associates a Teams meeting with an ACS chat thread
    /// Updates thread metadata with meeting information
    /// </summary>
    /// <param name="threadId">ACS chat thread ID</param>
    /// <param name="meetingLink">Teams meeting join URL</param>
    /// <param name="eventId">Calendar event ID</param>
    /// <returns>True if association was successful</returns>
    Task<bool> AssociateMeetingWithThreadAsync(string threadId, string meetingLink, string eventId);
}
