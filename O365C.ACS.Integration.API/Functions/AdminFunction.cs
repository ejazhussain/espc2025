// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using System;
using System.Threading.Tasks;

namespace O365C.ACS.Integration.API.Functions;

/// <summary>
/// Admin-related Azure Function endpoints
/// Handles configuration, endpoint discovery, and administrative operations
/// </summary>
public class AdminFunction
{
    private readonly ILogger<AdminFunction> _logger;
    private readonly IConfigurationService _configurationService;

    public AdminFunction(
        ILogger<AdminFunction> logger,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <summary>
    /// Gets the ACS service endpoint URL for client initialization
    /// GET /api/config/endpoint
    /// Returns configured endpoint URL for Azure Communication Services
    /// </summary>
    [Function("GetEndpoint")]
    public async Task<IActionResult> GetEndpointAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/getEndpoint")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("[ACS Admin] Received request to get ACS endpoint URL");

            var endpointResponse = await _configurationService.GetEndpointAsync();

            if (!endpointResponse.IsValid)
            {
                _logger.LogWarning("[ACS Admin] Invalid endpoint configuration: {ErrorMessage}", endpointResponse.ErrorMessage);
                return new BadRequestObjectResult(new
                {
                    error = "Invalid endpoint configuration",
                    message = endpointResponse.ErrorMessage ?? "Endpoint URL is not configured or invalid"
                });
            }

            _logger.LogInformation("[ACS Admin] Successfully retrieved ACS endpoint URL");

            // Return just the endpoint URL string to match TypeScript expectation
            return new OkObjectResult(endpointResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Admin] Error retrieving ACS endpoint URL");
            return new ObjectResult(new
            {
                error = "Failed to retrieve endpoint URL",
                message = "An error occurred while retrieving the ACS endpoint configuration"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}