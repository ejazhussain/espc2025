// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Meeting;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Service for Teams interoperability operations
/// Handles BYOI (Bring Your Own Identity) authentication for external users joining Teams meetings
/// </summary>
public class TeamsInteropService : ITeamsInteropService
{
    private readonly ITokenService _tokenService;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<TeamsInteropService> _logger;

    public TeamsInteropService(
        ITokenService tokenService,
        IChatRepository chatRepository,
        ILogger<TeamsInteropService> logger)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates ACS access token for external user to join Teams meeting
    /// Implements BYOI model - no Teams license required
    /// </summary>
    public async Task<InteropTokenResponse> GetMeetingAccessTokenAsync(InteropTokenRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation(
            "[Teams Interop Service] Generating access token for user {UserId} to join meeting",
            request.UserId.MaskForLogging());

        try
        {
            // Validate that user has access to the thread (customer verification)
            var hasAccess = await ValidateThreadAccessAsync(request.ThreadId, request.UserId);
            if (!hasAccess)
            {
                _logger.LogWarning(
                    "[Teams Interop Service] User {UserId} does not have access to thread {ThreadId}",
                    request.UserId.MaskForLogging(),
                    request.ThreadId.MaskForLogging());

                return new InteropTokenResponse
                {
                    Success = false,
                    ErrorMessage = "User does not have access to this meeting"
                };
            }

            // Generate ACS token with VoIP scope for calling
            // This enables external user to join Teams meeting without Teams license
            var scopes = new[]
            {
                CommunicationTokenScope.VoIP,  // Required for audio/video calling
                CommunicationTokenScope.Chat   // Optional for in-meeting chat
            };

            var tokenResponse = await _tokenService.CreateUserAndTokenAsync(scopes);

            _logger.LogInformation(
                "[Teams Interop Service] Successfully generated token for user {UserId}",
                request.UserId.MaskForLogging());

            return new InteropTokenResponse
            {
                Success = true,
                Token = tokenResponse.Token,
                UserId = tokenResponse.Identity,
                ExpiresOn = tokenResponse.ExpiresOn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Teams Interop Service] Failed to generate access token for user {UserId}",
                request.UserId.MaskForLogging());

            return new InteropTokenResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate access token: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Parses Teams meeting URL to extract meeting coordinates
    /// </summary>
    public async Task<MeetingJoinInfo> ParseTeamsMeetingLinkAsync(string meetingLink, string? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(meetingLink))
        {
            throw new ArgumentException("Meeting link is required", nameof(meetingLink));
        }

        _logger.LogInformation("[Teams Interop Service] Parsing Teams meeting link");

        try
        {
            // Teams meeting URL format: https://teams.microsoft.com/l/meetup-join/...
            var uri = new Uri(meetingLink);

            // Extract meeting ID from URL if present (for future use)
            string? meetingId = null;
            if (uri.AbsolutePath.Contains("meetup-join"))
            {
                var segments = uri.AbsolutePath.Split('/');
                if (segments.Length > 3)
                {
                    meetingId = segments[3]; // Extract meeting ID segment
                }
            }

            return await Task.FromResult(new MeetingJoinInfo
            {
                MeetingLink = meetingLink,
                MeetingId = meetingId,
                ThreadId = threadId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Interop Service] Failed to parse meeting link");
            throw;
        }
    }

    /// <summary>
    /// Validates if customer can access a specific meeting
    /// </summary>
    public async Task<ValidateMeetingAccessResponse> ValidateMeetingAccessAsync(ValidateMeetingAccessRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation(
            "[Teams Interop Service] Validating meeting access for user {UserId}",
            request.UserId.MaskForLogging());

        try
        {
            // Check if user is part of the chat thread
            var hasThreadAccess = await ValidateThreadAccessAsync(request.ThreadId, request.UserId);
            if (!hasThreadAccess)
            {
                _logger.LogWarning(
                    "[Teams Interop Service] User {UserId} does not have thread access to {ThreadId}",
                    request.UserId.MaskForLogging(),
                    request.ThreadId.MaskForLogging());
                
                return new ValidateMeetingAccessResponse
                {
                    CanAccess = false,
                    Reason = "User is not part of the associated chat thread"
                };
            }

            // Check if thread has an associated meeting
            var threadMetadata = await _chatRepository.GetThreadMetadataAsync(request.ThreadId);
            
            _logger.LogInformation(
                "[Teams Interop Service] Retrieved metadata for thread {ThreadId}: {MetadataCount} keys",
                request.ThreadId.MaskForLogging(),
                threadMetadata?.Count ?? 0);
            
            if (threadMetadata != null && threadMetadata.Count > 0)
            {
                _logger.LogInformation(
                    "[Teams Interop Service] Metadata keys: {Keys}",
                    string.Join(", ", threadMetadata.Keys));
            }
            
            // Note: Cosmos DB uses camelCase serialization, so check for "meetingJoinUrl" not "MeetingJoinUrl"
            if (threadMetadata == null || !threadMetadata.ContainsKey("meetingJoinUrl"))
            {
                _logger.LogWarning(
                    "[Teams Interop Service] No meeting metadata found for thread {ThreadId}",
                    request.ThreadId.MaskForLogging());
                
                return new ValidateMeetingAccessResponse
                {
                    CanAccess = false,
                    Reason = "No meeting is associated with this chat thread"
                };
            }

            var storedMeetingLink = threadMetadata["meetingJoinUrl"];
            
            // Verify the meeting link matches (or allow if not checking)
            if (!string.IsNullOrEmpty(request.MeetingLink) && 
                !storedMeetingLink.Equals(request.MeetingLink, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "[Teams Interop Service] Meeting link mismatch for thread {ThreadId}",
                    request.ThreadId.MaskForLogging());
                
                return new ValidateMeetingAccessResponse
                {
                    CanAccess = false,
                    Reason = "Meeting link does not match the scheduled meeting"
                };
            }

            var meetingInfo = await ParseTeamsMeetingLinkAsync(storedMeetingLink, request.ThreadId);

            return new ValidateMeetingAccessResponse
            {
                CanAccess = true,
                MeetingInfo = meetingInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Teams Interop Service] Failed to validate meeting access");
            return new ValidateMeetingAccessResponse
            {
                CanAccess = false,
                Reason = $"Validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Associates a Teams meeting with an ACS chat thread
    /// </summary>
    public async Task<bool> AssociateMeetingWithThreadAsync(string threadId, string meetingLink, string eventId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread ID is required", nameof(threadId));
        }

        if (string.IsNullOrWhiteSpace(meetingLink))
        {
            throw new ArgumentException("Meeting link is required", nameof(meetingLink));
        }

        _logger.LogInformation(
            "[Teams Interop Service] Associating meeting with thread {ThreadId}",
            threadId.MaskForLogging());

        try
        {
            var metadata = new Dictionary<string, string>
            {
                { "MeetingJoinUrl", meetingLink },
                { "MeetingEventId", eventId },
                { "MeetingAssociatedAt", DateTime.UtcNow.ToString("O") }
            };

            _logger.LogInformation(
                "[Teams Interop Service] Updating thread metadata with keys: {Keys}",
                string.Join(", ", metadata.Keys));

            var success = await _chatRepository.UpdateThreadMetadataAsync(threadId, metadata);

            if (success)
            {
                _logger.LogInformation(
                    "[Teams Interop Service] Successfully associated meeting with thread {ThreadId}",
                    threadId.MaskForLogging());
            }
            else
            {
                _logger.LogError(
                    "[Teams Interop Service] Failed to associate meeting - UpdateThreadMetadataAsync returned false for thread {ThreadId}",
                    threadId.MaskForLogging());
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Teams Interop Service] Failed to associate meeting with thread {ThreadId}",
                threadId.MaskForLogging());
            return false;
        }
    }

    /// <summary>
    /// Validates if user has access to the chat thread
    /// </summary>
    private async Task<bool> ValidateThreadAccessAsync(string threadId, string userId)
    {
        try
        {
            _logger.LogInformation(
                "[Teams Interop Service] Validating thread access for user {UserId} on thread {ThreadId}",
                userId.MaskForLogging(),
                threadId.MaskForLogging());

            // Check if user is a participant in the thread
            var participants = await _chatRepository.GetThreadParticipantsAsync(threadId);
            
            // For now, allow access if we can't retrieve participants (fail open for demo)
            if (participants == null)
            {
                _logger.LogWarning(
                    "[Teams Interop Service] Could not retrieve participants for thread {ThreadId}, allowing access",
                    threadId.MaskForLogging());
                return true;
            }

            _logger.LogInformation(
                "[Teams Interop Service] Found {Count} participants in thread",
                participants.Count);

            // Log incoming userId for comparison
            _logger.LogInformation(
                "[Teams Interop Service] Looking for userId: {UserId} (Length: {Length})",
                userId.MaskForLogging(),
                userId.Length);

            // Check if userId exists in participants
            // The userId format is typically "8:acs:..."
            var hasAccess = participants.Any(p => 
            {
                var identifier = p.Identifier;
                _logger.LogInformation(
                    "[Teams Interop Service] Checking participant - Identifier: {Identifier} (Length: {Length})",
                    identifier?.MaskForLogging() ?? "null",
                    identifier?.Length ?? 0);
                
                if (identifier != null)
                {
                    var exactMatch = identifier.Equals(userId, StringComparison.OrdinalIgnoreCase);
                    var containsMatch = identifier.Contains(userId, StringComparison.OrdinalIgnoreCase);
                    
                    _logger.LogInformation(
                        "[Teams Interop Service] Match results - ExactMatch: {ExactMatch}, ContainsMatch: {ContainsMatch}",
                        exactMatch,
                        containsMatch);
                    
                    return exactMatch || containsMatch;
                }
                
                return false;
            });

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "[Teams Interop Service] User {UserId} not found in thread participants. For demo purposes, allowing access anyway.",
                    userId.MaskForLogging());
                // TODO: For production, return false here
                return true; // Allow for demo
            }

            return hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[Teams Interop Service] Error validating thread access, allowing access");
            // Fail open for now - in production, you might want to fail closed
            return true;
        }
    }
}
