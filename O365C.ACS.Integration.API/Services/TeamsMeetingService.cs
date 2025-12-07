// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Meeting;
using O365C.ACS.Integration.API.Models.Chat;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Service for managing Teams meetings via Microsoft Graph Calendar API
/// Implements creation, retrieval, and management of online meetings
/// </summary>
public class TeamsMeetingService : ITeamsMeetingService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<TeamsMeetingService> _logger;
    private readonly IChatRepository _chatRepository;
    private readonly IChatService _chatService;

    public TeamsMeetingService(
        GraphServiceClient graphServiceClient,
        ILogger<TeamsMeetingService> logger,
        IChatRepository chatRepository,
        IChatService chatService)
    {
        _graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
    }

    /// <summary>
    /// Creates a new Teams meeting using Microsoft Graph Calendar API
    /// </summary>
    public async Task<CreateMeetingResponse> CreateTeamsMeetingAsync(CreateMeetingRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation(
            "[Teams Meeting Service] Creating meeting for thread {ThreadId} with agent {AgentEmail}",
            request.ThreadId.MaskForLogging(),
            request.AgentEmail);

        try
        {
            // Build meeting attendees list
            var attendees = new List<Attendee>();

            // Add customer if email provided
            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                attendees.Add(new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = request.CustomerEmail,
                        Name = request.CustomerName
                    },
                    Type = AttendeeType.Required
                });
            }

            // Create event with Teams meeting
            var newEvent = new Event
            {
                Subject = request.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = BuildMeetingBody(request)
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = request.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = request.TimeZone
                },
                End = new DateTimeTimeZone
                {
                    DateTime = request.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = request.TimeZone
                },
                Attendees = attendees,
                IsOnlineMeeting = true,
                OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness,
                AllowNewTimeProposals = true
            };

            // Create the event in the agent's calendar
            var createdEvent = await _graphServiceClient
                .Users[request.AgentEmail]
                .Calendar
                .Events
                .PostAsync(newEvent);

            if (createdEvent?.OnlineMeeting == null)
            {
                _logger.LogError("[Teams Meeting Service] Failed to create Teams meeting - no online meeting info returned");
                return new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to create Teams meeting"
                };
            }

            _logger.LogInformation(
                "[Teams Meeting Service] Successfully created meeting with ID {EventId}",
                createdEvent.Id?.MaskForLogging());

            //TODO - needs to be removed  - duplicate call
            await StoreMeetingMetadataAsync(request.ThreadId, createdEvent.Id!, createdEvent.OnlineMeeting.JoinUrl!);

            // Send meeting link to customer via chat
            await SendMeetingLinkToCustomerAsync(
                request.ThreadId, 
                createdEvent.OnlineMeeting.JoinUrl!, 
                request.StartDateTime,
                request.CustomerName);

            return new CreateMeetingResponse
            {
                Success = true,
                EventId = createdEvent.Id,
                JoinUrl = createdEvent.OnlineMeeting.JoinUrl,
                ConferenceId = createdEvent.OnlineMeeting.ConferenceId,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                ThreadId = request.ThreadId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Meeting Service] Failed to create Teams meeting");
            return new CreateMeetingResponse
            {
                Success = false,
                ErrorMessage = $"Failed to create meeting: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Retrieves details of an existing Teams meeting
    /// </summary>
    public async Task<MeetingDetails?> GetMeetingDetailsAsync(string eventId, string organizerEmail)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Event ID is required", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(organizerEmail))
        {
            throw new ArgumentException("Organizer email is required", nameof(organizerEmail));
        }

        _logger.LogInformation(
            "[Teams Meeting Service] Retrieving meeting details for event {EventId}",
            eventId.MaskForLogging());

        try
        {
            var eventDetails = await _graphServiceClient
                .Users[organizerEmail]
                .Events[eventId]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "subject",
                        "body",
                        "start",
                        "end",
                        "attendees",
                        "organizer",
                        "isOnlineMeeting",
                        "onlineMeeting",
                        "onlineMeetingProvider"
                    };
                });

            if (eventDetails == null)
            {
                _logger.LogWarning("[Teams Meeting Service] Event not found: {EventId}", eventId.MaskForLogging());
                return null;
            }

            var meetingDetails = new MeetingDetails
            {
                EventId = eventDetails.Id!,
                Subject = eventDetails.Subject ?? "Teams Meeting",
                Description = eventDetails.Body?.Content,
                StartDateTime = DateTime.Parse(eventDetails.Start!.DateTime!),
                EndDateTime = DateTime.Parse(eventDetails.End!.DateTime!),
                JoinUrl = eventDetails.OnlineMeeting?.JoinUrl ?? string.Empty,
                ConferenceId = eventDetails.OnlineMeeting?.ConferenceId,
                TollNumber = eventDetails.OnlineMeeting?.TollNumber,
                OrganizerEmail = eventDetails.Organizer?.EmailAddress?.Address ?? organizerEmail,
                Attendees = eventDetails.Attendees?
                    .Select(a => new MeetingAttendee
                    {
                        Name = a.EmailAddress?.Name ?? string.Empty,
                        Email = a.EmailAddress?.Address ?? string.Empty,
                        Type = a.Type?.ToString() ?? "Required",
                        ResponseStatus = a.Status?.Response?.ToString()
                    })
                    .ToList() ?? new List<MeetingAttendee>()
            };

            return meetingDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Meeting Service] Failed to retrieve meeting details");
            return null;
        }
    }

    /// <summary>
    /// Cancels a Teams meeting
    /// </summary>
    public async Task<bool> CancelMeetingAsync(string eventId, string organizerEmail, string? cancellationMessage = null)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Event ID is required", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(organizerEmail))
        {
            throw new ArgumentException("Organizer email is required", nameof(organizerEmail));
        }

        _logger.LogInformation(
            "[Teams Meeting Service] Cancelling meeting {EventId}",
            eventId.MaskForLogging());

        try
        {
            await _graphServiceClient
                .Users[organizerEmail]
                .Events[eventId]
                .Cancel
                .PostAsync(new Microsoft.Graph.Users.Item.Events.Item.Cancel.CancelPostRequestBody
                {
                    Comment = cancellationMessage ?? "This meeting has been cancelled."
                });

            _logger.LogInformation(
                "[Teams Meeting Service] Successfully cancelled meeting {EventId}",
                eventId.MaskForLogging());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Meeting Service] Failed to cancel meeting");
            return false;
        }
    }

    /// <summary>
    /// Updates an existing Teams meeting
    /// </summary>
    public async Task<CreateMeetingResponse> UpdateMeetingAsync(
        string eventId,
        string organizerEmail,
        CreateMeetingRequest request)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Event ID is required", nameof(eventId));
        }

        _logger.LogInformation(
            "[Teams Meeting Service] Updating meeting {EventId}",
            eventId.MaskForLogging());

        try
        {
            var updatedEvent = new Event
            {
                Subject = request.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = BuildMeetingBody(request)
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = request.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = request.TimeZone
                },
                End = new DateTimeTimeZone
                {
                    DateTime = request.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = request.TimeZone
                }
            };

            var result = await _graphServiceClient
                .Users[organizerEmail]
                .Events[eventId]
                .PatchAsync(updatedEvent);

            if (result?.OnlineMeeting == null)
            {
                return new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to update meeting"
                };
            }

            return new CreateMeetingResponse
            {
                Success = true,
                EventId = result.Id,
                JoinUrl = result.OnlineMeeting.JoinUrl,
                ConferenceId = result.OnlineMeeting.ConferenceId,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                ThreadId = request.ThreadId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Meeting Service] Failed to update meeting");
            return new CreateMeetingResponse
            {
                Success = false,
                ErrorMessage = $"Failed to update meeting: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Builds HTML body content for the meeting
    /// </summary>
    private string BuildMeetingBody(CreateMeetingRequest request)
    {
        var body = $"<div><strong>Support Session</strong></div>";
        body += $"<div>Customer: {request.CustomerName}</div>";

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            body += $"<div><br/>{request.Description}</div>";
        }

        body += "<div><br/><em>This meeting was scheduled via the Customer Support Platform.</em></div>";

        return body;
    }

    /// <summary>
    /// Stores meeting metadata in chat thread
    /// </summary>
    private async Task StoreMeetingMetadataAsync(string threadId, string eventId, string joinUrl)
    {
        try
        {
            // Store meeting info in chat repository for future reference
            await _chatRepository.UpdateThreadMetadataAsync(threadId, new Dictionary<string, string>
            {
                { "MeetingEventId", eventId },
                { "MeetingJoinUrl", joinUrl },
                { "MeetingScheduledAt", DateTime.UtcNow.ToString("O") }
            });

            _logger.LogInformation(
                "[Teams Meeting Service] Stored meeting metadata for thread {ThreadId}",
                threadId.MaskForLogging());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Teams Meeting Service] Failed to store meeting metadata");
            // Non-critical operation, don't throw
        }
    }

    /// <summary>
    /// Sends meeting link to customer via ACS chat
    /// </summary>
    private async Task SendMeetingLinkToCustomerAsync(string threadId, string joinUrl, DateTime startDateTime, string customerName)
    {
        try
        {
            _logger.LogInformation(
                "[Teams Meeting Service] Sending meeting link to customer in thread {ThreadId}",
                threadId.MaskForLogging());

            var startTime = startDateTime.ToString("MMMM d, yyyy 'at' h:mm tt 'UTC'");
            
           
            var message = $"<div style='padding: 12px; border-left: 4px solid #6264A7; background-color: #F5F5F5;'>" +
                         $"<p style='margin: 0 0 8px 0;'><strong style='font-size: 16px;'>🎥 Teams Meeting Scheduled</strong></p>" +
                         $"<p style='margin: 0 0 8px 0;'>Hi {customerName},</p>" +
                         $"<p style='margin: 0 0 12px 0;'>I've scheduled a Teams meeting for us!</p>" +
                         $"<div style='background-color: white; padding: 12px; border-radius: 4px; margin-bottom: 12px;'>" +
                         $"<p style='margin: 0 0 4px 0;'><strong>📅 Time:</strong> {startTime}</p>" +
                         $"<p style='margin: 0;'><strong>⏱️ Duration:</strong> 30 minutes</p>" +
                         $"</div>" +
                         $"<p style='margin: 0 0 8px 0;'><a href='{joinUrl}' style='display: inline-block; background-color: #6264A7; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Join Meeting</a></p>" +
                         $"<p style='margin: 0; color: #666;'>See you there! 👋</p>" +
                         $"</div>";

            // Get agent work item to retrieve the assigned agent's ACS User ID
            var workItem = await _chatRepository.GetAgentWorkItemAsync(threadId);
            
            if (workItem == null)
            {
                _logger.LogWarning("[Teams Meeting Service] Could not find work item for thread {ThreadId} to send meeting link", threadId.MaskForLogging());
                return;
            }

            // Get agent ACS User ID from work item (AssignedAgentId contains the ACS identity like "8:acs:...")
            var agentAcsUserId = workItem.AssignedAgentId;
            
            if (string.IsNullOrWhiteSpace(agentAcsUserId))
            {
                _logger.LogWarning("[Teams Meeting Service] Work item {ThreadId} has no assigned agent, cannot send meeting link", threadId.MaskForLogging());
                return;
            }

            var sendMessageRequest = new SendMessageRequest
            {
                ThreadId = threadId,
                UserId = agentAcsUserId,
                DisplayName = workItem.AssignedAgentName ?? "Support Agent",
                Message = message,
                MessageType = "html"  // Use HTML for rich formatting
            };

            var result = await _chatService.SendMessageAsync(sendMessageRequest);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Teams Meeting Service] Successfully sent meeting link to customer in thread {ThreadId}",
                    threadId.MaskForLogging());
            }
            else
            {
                _logger.LogWarning(
                    "[Teams Meeting Service] Failed to send meeting link: {Error}",
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Meeting Service] Error sending meeting link to customer");
            // Don't throw - meeting was created successfully
        }
    }
}
