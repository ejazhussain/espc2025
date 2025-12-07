// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Extensions;

namespace O365C.ACS.Integration.API.Functions;

/// <summary>
/// Debug functions for troubleshooting
/// </summary>
public class DebugFunction
{
    private readonly ILogger<DebugFunction> _logger;
    private readonly IChatRepository _chatRepository;

    public DebugFunction(
        ILogger<DebugFunction> logger,
        IChatRepository chatRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
    }

    /// <summary>
    /// Gets thread metadata for debugging
    /// GET /api/debug/thread/{threadId}/metadata
    /// </summary>
    [Function("GetThreadMetadata")]
    public async Task<IActionResult> GetThreadMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debug/thread/{threadId}/metadata")] HttpRequest req,
        string threadId)
    {
        try
        {
            _logger.LogInformation("[Debug Function] Getting metadata for thread {ThreadId}", threadId.MaskForLogging());

            // Get work item
            var workItem = await _chatRepository.GetAgentWorkItemAsync(threadId);
            if (workItem == null)
            {
                return new NotFoundObjectResult(new
                {
                    success = false,
                    error = "Thread not found in database",
                    threadId = threadId
                });
            }

            // Get metadata
            var metadata = await _chatRepository.GetThreadMetadataAsync(threadId);

            return new OkObjectResult(new
            {
                success = true,
                threadId = threadId,
                workItemExists = true,
                metadata = metadata ?? new Dictionary<string, string>(),
                metadataCount = metadata?.Count ?? 0,
                workItem = new
                {
                    workItem.Id,
                    workItem.Status,
                    workItem.CustomerName,
                    workItem.CustomerId,
                    workItem.AssignedAgentId,
                    workItem.CreatedAt,
                    workItem.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug Function] Failed to get thread metadata");
            return new ObjectResult(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Gets thread participants for debugging
    /// GET /api/debug/thread/{threadId}/participants
    /// </summary>
    [Function("GetThreadParticipants")]
    public async Task<IActionResult> GetThreadParticipantsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debug/thread/{threadId}/participants")] HttpRequest req,
        string threadId)
    {
        try
        {
            _logger.LogInformation("[Debug Function] Getting participants for thread {ThreadId}", threadId.MaskForLogging());

            var participants = await _chatRepository.GetThreadParticipantsAsync(threadId);

            return new OkObjectResult(new
            {
                success = true,
                threadId = threadId,
                participants = participants?.Select(p => new
                {
                    p.Identifier,
                    p.DisplayName
                }).ToList(),
                participantCount = participants?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug Function] Failed to get thread participants");
            return new ObjectResult(new
            {
                success = false,
                error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }
}
