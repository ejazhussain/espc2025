// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace O365C.ACS.Integration.API.Models.Meeting;

/// <summary>
/// Request for generating ACS token to join Teams meeting
/// </summary>
public class InteropTokenRequest
{
    /// <summary>
    /// Customer user ID or identifier
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for customer in the meeting
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Thread ID to validate customer access
    /// </summary>
    [Required]
    public string ThreadId { get; set; } = string.Empty;
}

/// <summary>
/// Response containing ACS token for Teams meeting interoperability
/// </summary>
public class InteropTokenResponse
{
    /// <summary>
    /// Indicates if token generation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ACS access token
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// ACS user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Error message if token generation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information required to join a Teams meeting
/// </summary>
public class MeetingJoinInfo
{
    /// <summary>
    /// Teams meeting join URL
    /// </summary>
    public string MeetingLink { get; set; } = string.Empty;

    /// <summary>
    /// Meeting ID (extracted from URL if available)
    /// </summary>
    public string? MeetingId { get; set; }

    /// <summary>
    /// Meeting passcode (if required)
    /// </summary>
    public string? Passcode { get; set; }

    /// <summary>
    /// Thread ID associated with this meeting
    /// </summary>
    public string? ThreadId { get; set; }
}

/// <summary>
/// Request to validate customer access to a meeting
/// </summary>
public class ValidateMeetingAccessRequest
{
    /// <summary>
    /// Thread ID
    /// </summary>
    [Required]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Customer user ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Meeting join URL
    /// </summary>
    [Required]
    public string MeetingLink { get; set; } = string.Empty;
}

/// <summary>
/// Response for meeting access validation
/// </summary>
public class ValidateMeetingAccessResponse
{
    /// <summary>
    /// Indicates if customer can access the meeting
    /// </summary>
    public bool CanAccess { get; set; }

    /// <summary>
    /// Reason if access is denied
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Meeting details if access is granted
    /// </summary>
    public MeetingJoinInfo? MeetingInfo { get; set; }
}
