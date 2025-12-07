// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Interfaces;
using System.Text.Json;

namespace O365C.ACS.Integration.API.Functions
{
    /// <summary>
    /// Azure Functions for Azure Communication Services token operations
    /// Handles secure token generation and management following Azure best practices
    /// 
    /// Features:
    /// - User token generation
    /// - Token refresh
    /// - Enterprise logging and monitoring
    /// 
    /// Reference: https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/access-tokens
    /// </summary>
    public class TokenFunction
    {
        private readonly ILogger<TokenFunction> _logger;
        private readonly ITokenService _tokenService;

        public TokenFunction(
            ILogger<TokenFunction> logger,
            ITokenService tokenService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        /// <summary>
        /// Creates a new ACS user and token with specified scopes
        /// POST /api/acs/token
        /// 
        /// Query parameters:
        /// - scope: Comma-separated list of scopes (default: chat)
        /// 
        /// Returns: ACSUserTokenResponse with user identity and access token

        [Function("CreateUserToken")]
        public async Task<IActionResult> CreateUserTokenAsync(
       [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "token/createUserToken")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Token] Received request to create new user and token");

                // Parse scopes from query parameters (default to chat)
                var scopeParam = req.Query["scope"].FirstOrDefault() ?? "chat";
                var scopes = scopeParam.ParseTokenScopes();

                // Validate scopes
                if (!scopes.Any())
                {
                    _logger.LogWarning("[ACS Token] No valid scopes provided, using default 'chat' scope");
                    scopes = new[] { CommunicationTokenScope.Chat };
                }

                // Create user and token
                var result = await _tokenService.CreateUserAndTokenAsync(scopes);                

                _logger.LogInformation("[ACS Token] Successfully created user and token for user: {UserId}", result.Identity);

                return new OkObjectResult(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "[ACS Token] Invalid request parameters");
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Token] Unexpected error during token creation");
                return new ObjectResult(new { error = "Internal server error" })
                {
                    StatusCode = 500
                };
            }
        }



        /// <summary>
        /// Refreshes an existing access token
        /// POST /api/token/refresh
        /// 
        /// Body: JSON with existing token
        /// Returns: Response with new token
        /// </summary>
        [Function("RefreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "token/refresh")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Token] Received request to refresh access token");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var requestData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (requestData == null || !requestData.TryGetValue("token", out var token) || string.IsNullOrWhiteSpace(token))
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        errorMessage = "Token is required for refresh"
                    });
                }

                // TODO: Implement token refresh using ITokenService
                // var response = await _tokenService.RefreshTokenAsync(request);

                _logger.LogInformation("[ACS Token] Token refresh placeholder - implementation pending");

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Token refresh endpoint - implementation pending"
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ACS Token] Invalid JSON in request body");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    errorMessage = "Invalid request format"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Token] Unexpected error during token refresh");
                return new ObjectResult(new
                {
                    success = false,
                    errorMessage = "Internal server error"
                })
                {
                    StatusCode = 500
                };
            }
        }

        /// <summary>
        /// Gets token information for a user
        /// GET /api/acs/token/info?userId={userId}
        /// 
        /// Returns: Token information and validity status
        /// </summary>
        [Function("GetTokenInfo")]
        public async Task<IActionResult> GetTokenInfoAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "token/info")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ACS Token] Received request to get token info");

                string? userId = req.Query["userId"];

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        errorMessage = "UserId is required"
                    });
                }
                
                var response = await _tokenService.GetTokenForUserAsync(userId, new[] { CommunicationTokenScope.Chat });

                _logger.LogInformation("[ACS Token] Token info placeholder for user: {UserId}", userId.MaskForLogging());

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACS Token] Unexpected error during token info retrieval");
                return new ObjectResult(new
                {
                    success = false,
                    errorMessage = "Internal server error"
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}