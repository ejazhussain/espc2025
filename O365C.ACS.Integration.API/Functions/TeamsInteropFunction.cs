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
/// Azure Functions for Teams interoperability
/// Provides endpoints for external users (BYOI) to join Teams meetings via ACS
/// </summary>
public class TeamsInteropFunction
{
    private readonly ILogger<TeamsInteropFunction> _logger;
    private readonly ITeamsInteropService _interopService;

    public TeamsInteropFunction(
        ILogger<TeamsInteropFunction> logger,
        ITeamsInteropService interopService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interopService = interopService ?? throw new ArgumentNullException(nameof(interopService));
    }

    /// <summary>
    /// Generates ACS token for customer to join Teams meeting
    /// POST /api/teamsinterop/token
    /// 
    /// This implements the BYOI (Bring Your Own Identity) model
    /// No Teams license required for external users
    /// </summary>
    [Function("GetTeamsMeetingToken")]
    public async Task<IActionResult> GetMeetingTokenAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "teamsinterop/token")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[Teams Interop Function] Received request for meeting access token");

            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<InteropTokenRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new InteropTokenResponse
                {
                    Success = false,
                    ErrorMessage = "Request body is required"
                });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return new BadRequestObjectResult(new InteropTokenResponse
                {
                    Success = false,
                    ErrorMessage = "UserId is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.ThreadId))
            {
                return new BadRequestObjectResult(new InteropTokenResponse
                {
                    Success = false,
                    ErrorMessage = "ThreadId is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new BadRequestObjectResult(new InteropTokenResponse
                {
                    Success = false,
                    ErrorMessage = "DisplayName is required"
                });
            }

            _logger.LogInformation(
                "[Teams Interop Function] Generating token for user {UserId}",
                request.UserId.MaskForLogging());

            var response = await _interopService.GetMeetingAccessTokenAsync(request);

            return response.Success
                ? new OkObjectResult(response)
                : new ObjectResult(response) { StatusCode = response.ErrorMessage?.Contains("access") == true ? 403 : 400 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Interop Function] Failed to generate meeting access token");
            return new ObjectResult(new InteropTokenResponse
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
    /// Validates if customer can access a meeting
    /// POST /api/teamsinterop/validate
    /// </summary>
    [Function("ValidateMeetingAccess")]
    public async Task<IActionResult> ValidateMeetingAccessAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "teamsinterop/validate")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[Teams Interop Function] Received request to validate meeting access");

            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ValidateMeetingAccessRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new ValidateMeetingAccessResponse
                {
                    CanAccess = false,
                    Reason = "Request body is required"
                });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.ThreadId) || string.IsNullOrWhiteSpace(request.UserId))
            {
                return new BadRequestObjectResult(new ValidateMeetingAccessResponse
                {
                    CanAccess = false,
                    Reason = "ThreadId and UserId are required"
                });
            }

            _logger.LogInformation(
                "[Teams Interop Function] Validating access for user {UserId} to thread {ThreadId}",
                request.UserId.MaskForLogging(),
                request.ThreadId.MaskForLogging());

            var response = await _interopService.ValidateMeetingAccessAsync(request);

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Interop Function] Failed to validate meeting access");
            return new ObjectResult(new ValidateMeetingAccessResponse
            {
                CanAccess = false,
                Reason = $"Validation error: {ex.Message}"
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Parses Teams meeting link to extract meeting information
    /// GET /api/teamsinterop/meeting-info
    /// </summary>
    [Function("GetMeetingInfo")]
    public async Task<IActionResult> GetMeetingInfoAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teamsinterop/meeting-info")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[Teams Interop Function] Received request to parse meeting link");

            var meetingLink = req.Query["meetingLink"].ToString();
            var threadId = req.Query["threadId"].ToString();

            if (string.IsNullOrWhiteSpace(meetingLink))
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    errorMessage = "meetingLink query parameter is required"
                });
            }

            var meetingInfo = await _interopService.ParseTeamsMeetingLinkAsync(meetingLink, threadId);

            return new OkObjectResult(new
            {
                success = true,
                meetingInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Interop Function] Failed to parse meeting link");
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
}
