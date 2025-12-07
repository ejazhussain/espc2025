// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Meeting;
using O365C.ACS.Integration.API.Extensions;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Functions;

/// <summary>
/// Azure Functions for Teams meeting management
/// Provides HTTP endpoints for creating, retrieving, updating, and canceling Teams meetings
/// </summary>
public class MeetingFunction
{
    private readonly ILogger<MeetingFunction> _logger;
    private readonly ITeamsMeetingService _meetingService;
    private readonly ITeamsInteropService _interopService;

    public MeetingFunction(
        ILogger<MeetingFunction> logger,
        ITeamsMeetingService meetingService,
        ITeamsInteropService interopService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meetingService = meetingService ?? throw new ArgumentNullException(nameof(meetingService));
        _interopService = interopService ?? throw new ArgumentNullException(nameof(interopService));
    }

    /// <summary>
    /// Creates a new Teams meeting
    /// POST /api/meeting/create
    /// </summary>
    [Function("CreateTeamsMeeting")]
    public async Task<IActionResult> CreateMeetingAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "meeting/create")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[Meeting Function] Received request to create Teams meeting");

            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateMeetingRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "Request body is required"
                });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.ThreadId))
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "ThreadId is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.AgentEmail))
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "AgentEmail is required"
                });
            }

            if (request.StartDateTime >= request.EndDateTime)
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "EndDateTime must be after StartDateTime"
                });
            }

            _logger.LogInformation(
                "[Meeting Function] Creating meeting for thread {ThreadId} with agent {AgentEmail}",
                request.ThreadId.MaskForLogging(),
                request.AgentEmail);

            // Create the meeting
            var response = await _meetingService.CreateTeamsMeetingAsync(request);

            if (response.Success && !string.IsNullOrEmpty(response.JoinUrl) && !string.IsNullOrEmpty(response.EventId))
            {
                // Associate meeting with chat thread for interoperability
                var associationSuccess = await _interopService.AssociateMeetingWithThreadAsync(
                    request.ThreadId,
                    response.JoinUrl,
                    response.EventId);

                if (!associationSuccess)
                {
                    _logger.LogWarning(
                        "[Meeting Function] Meeting created but failed to associate with thread {ThreadId}",
                        request.ThreadId.MaskForLogging());
                    
                    // Still return success for meeting creation but log warning
                    response.ErrorMessage = "Meeting created but association with chat thread failed";
                }
                else
                {
                    _logger.LogInformation(
                        "[Meeting Function] Successfully created meeting with ID {EventId} and associated with thread",
                        response.EventId.MaskForLogging());
                }
            }

            return response.Success
                ? new OkObjectResult(response)
                : new BadRequestObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Meeting Function] Failed to create Teams meeting");
            return new ObjectResult(new CreateMeetingResponse
            {
                Success = false,
                ErrorMessage = $"An error occurred: {ex.Message}"
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Gets meeting details
    /// GET /api/meeting/{eventId}
    /// </summary>
    [Function("GetMeetingDetails")]
    public async Task<IActionResult> GetMeetingDetailsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meeting/{eventId}")] HttpRequest req,
        string eventId)
    {
        try
        {
            _logger.LogInformation(
                "[Meeting Function] Received request to get meeting details for {EventId}",
                eventId.MaskForLogging());

            // Get organizer email from query parameters
            var organizerEmail = req.Query["organizerEmail"].ToString();
            if (string.IsNullOrWhiteSpace(organizerEmail))
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    errorMessage = "organizerEmail query parameter is required"
                });
            }

            var meetingDetails = await _meetingService.GetMeetingDetailsAsync(eventId, organizerEmail);

            if (meetingDetails == null)
            {
                return new NotFoundObjectResult(new
                {
                    success = false,
                    errorMessage = "Meeting not found"
                });
            }

            return new OkObjectResult(meetingDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Meeting Function] Failed to get meeting details");
            return new ObjectResult(new
            {
                success = false,
                errorMessage = $"An error occurred: {ex.Message}"
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Cancels a Teams meeting
    /// DELETE /api/meeting/{eventId}
    /// </summary>
    [Function("CancelMeeting")]
    public async Task<IActionResult> CancelMeetingAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "meeting/{eventId}")] HttpRequest req,
        string eventId)
    {
        try
        {
            _logger.LogInformation(
                "[Meeting Function] Received request to cancel meeting {EventId}",
                eventId.MaskForLogging());

            // Get organizer email and optional cancellation message
            var organizerEmail = req.Query["organizerEmail"].ToString();
            if (string.IsNullOrWhiteSpace(organizerEmail))
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    errorMessage = "organizerEmail query parameter is required"
                });
            }

            var cancellationMessage = req.Query["message"].ToString();

            var success = await _meetingService.CancelMeetingAsync(eventId, organizerEmail, cancellationMessage);

            if (success)
            {
                _logger.LogInformation(
                    "[Meeting Function] Successfully cancelled meeting {EventId}",
                    eventId.MaskForLogging());

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Meeting cancelled successfully"
                });
            }

            return new ObjectResult(new
            {
                success = false,
                errorMessage = "Failed to cancel meeting"
            })
            {
                StatusCode = 500
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Meeting Function] Failed to cancel meeting");
            return new ObjectResult(new
            {
                success = false,
                errorMessage = $"An error occurred: {ex.Message}"
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Updates an existing Teams meeting
    /// PUT /api/meeting/{eventId}
    /// </summary>
    [Function("UpdateMeeting")]
    public async Task<IActionResult> UpdateMeetingAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "meeting/{eventId}")] HttpRequest req,
        string eventId)
    {
        try
        {
            _logger.LogInformation(
                "[Meeting Function] Received request to update meeting {EventId}",
                eventId.MaskForLogging());

            // Get organizer email from query
            var organizerEmail = req.Query["organizerEmail"].ToString();
            if (string.IsNullOrWhiteSpace(organizerEmail))
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "organizerEmail query parameter is required"
                });
            }

            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateMeetingRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new CreateMeetingResponse
                {
                    Success = false,
                    ErrorMessage = "Request body is required"
                });
            }

            var response = await _meetingService.UpdateMeetingAsync(eventId, organizerEmail, request);

            return response.Success
                ? new OkObjectResult(response)
                : new BadRequestObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Meeting Function] Failed to update meeting");
            return new ObjectResult(new CreateMeetingResponse
            {
                Success = false,
                ErrorMessage = $"An error occurred: {ex.Message}"
            })
            {
                StatusCode = 500
            };
        }
    }
}
