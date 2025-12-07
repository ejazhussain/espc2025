// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Email;

namespace O365C.ACS.Integration.API.Functions;

public class EmailFunction
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailFunction> _logger;

    public EmailFunction(IEmailService emailService, ILogger<EmailFunction> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("SendTranscriptEmail")]
    public async Task<IActionResult> SendTranscriptEmailAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "email/transcript")] HttpRequest req)
    {
        _logger.LogInformation("[Email Function] Processing send transcript email request");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("[Email Function] Empty request body received");
                return new BadRequestObjectResult(new { error = "Request body cannot be empty" });
            }

            SendTranscriptEmailRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<SendTranscriptEmailRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[Email Function] Failed to deserialize request body");
                return new BadRequestObjectResult(new { error = "Invalid JSON format in request body" });
            }

            if (request is null)
            {
                _logger.LogWarning("[Email Function] Deserialized request is null");
                return new BadRequestObjectResult(new { error = "Invalid request data" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                return new BadRequestObjectResult(new { error = "CustomerEmail is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return new BadRequestObjectResult(new { error = "CustomerName is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ThreadId))
            {
                return new BadRequestObjectResult(new { error = "ThreadId is required" });
            }

            var response = await _emailService.SendTranscriptEmailAsync(request);
            
            if (response.Success)
            {
                _logger.LogInformation("[Email Function] Successfully sent transcript email for thread {ThreadId}", request.ThreadId);
                return new OkObjectResult(response);
            }
            else
            {
                _logger.LogError("[Email Function] Failed to send transcript email: {Message}", response.Message);
                return new BadRequestObjectResult(response);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "[Email Function] Validation error while sending transcript email");
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email Function] Unexpected error while sending transcript email");
            return new StatusCodeResult(500);
        }
    }
}