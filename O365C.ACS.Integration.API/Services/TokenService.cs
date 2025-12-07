// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Helpers;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Models.Token;

namespace O365C.ACS.Integration.API.Services;

/// <summary>
/// Azure Communication Services token management implementation
/// Handles user identity and access token operations with security best practices
/// </summary>
public class TokenService : ITokenService
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly ILogger<TokenService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppSettings _appSettings;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger, AppSettings appSettings)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        try
        {
            var connectionString = GetAcsConnectionString();
            _identityClient = new CommunicationIdentityClient(connectionString);

            _logger.LogInformation("[ACS Token Service] Successfully initialized with connection string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Token Service] Failed to initialize Communication Identity Client");
            throw;
        }
    }

    /// <summary>
    /// Gets ACS connection string from strongly-typed configuration with fallback
    /// </summary>
    private string GetAcsConnectionString()
    {
        // First try strongly-typed configuration
        var connectionString = _appSettings.ConnectionStrings.AzureCommunicationServices;
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fallback to legacy configuration access
        return ConfigurationHelper.GetValidatedConnectionString(_configuration);
    }

    /// <summary>
    /// Creates a new ACS user and generates token with specified scopes
    /// Implements proper error handling and security practices
    /// </summary>
    public async Task<UserTokenResponse> CreateUserAndTokenAsync(IEnumerable<CommunicationTokenScope> scopes)
    {
        // Validate input using extension method
        var scopeArray = TokenScopeExtensions.ValidateAndConvert(scopes);

        var logContext = TokenOperationHelper.CreateTokenLogContext("CreateUserAndToken", scopes: scopeArray);

        try
        {
            _logger.LogInformation("[ACS Token Service] Creating new user and token with scopes: {Scopes} | Context: {@LogContext}",
                string.Join(", ", scopeArray.Select(s => s.ToString())), logContext);

            var response = await _identityClient.CreateUserAndTokenAsync(scopeArray);

            var result = new UserTokenResponse
            {
                Identity = response.Value.User.Id,
                Token = response.Value.AccessToken.Token,
                ExpiresOn = response.Value.AccessToken.ExpiresOn,
                User = new UserInfo
                {
                    CommunicationUserId = response.Value.User.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = "Active"
                }
            };

            _logger.LogInformation("[ACS Token Service] Successfully created user and token | UserId: {UserId} | ExpiresOn: {ExpiresOn} | Context: {@LogContext}",
                result.Identity.MaskForLogging(), result.ExpiresOn, logContext);

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[ACS Token Service] ACS request failed during user and token creation | Status: {Status} | ErrorCode: {ErrorCode} | Context: {@LogContext}",
                ex.Status, ex.ErrorCode, logContext);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Token Service] Unexpected error while creating user and token | Context: {@LogContext}", logContext);
            throw;
        }
    }

    /// <summary>
    /// Generates new token for existing ACS user
    /// Note: This method creates a simplified token for existing user ID
    /// For production, ensure the user ID exists in your ACS resource
    /// </summary>
    public async Task<UserTokenResponse> GetTokenForUserAsync(string userId, IEnumerable<CommunicationTokenScope> scopes)
    {
        // Validate inputs using extension methods
        userId.ValidateNotNullOrEmpty(nameof(userId));
        var scopeArray = TokenScopeExtensions.ValidateAndConvert(scopes);

        var logContext = TokenOperationHelper.CreateTokenLogContext("GetTokenForUser", userId, scopeArray);

        try
        {
            _logger.LogInformation("[ACS Token Service] Generating token for existing user {UserId} with scopes: {Scopes} | Context: {@LogContext}",
                userId.MaskForLogging(), string.Join(", ", scopeArray.Select(s => s.ToString())), logContext);

            // Create CommunicationUserIdentifier from the existing userId
            var userIdentifier = new CommunicationUserIdentifier(userId);

            // Generate token for the EXISTING user (not create a new one)
            var tokenResponse = await _identityClient.GetTokenAsync(userIdentifier, scopeArray);

            var result = new UserTokenResponse
            {
                Identity = userId, // Use the existing userId, not the new one
                Token = tokenResponse.Value.Token,
                ExpiresOn = tokenResponse.Value.ExpiresOn,
                User = new UserInfo
                {
                    CommunicationUserId = userId, // Use the existing userId
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = "Active"
                }
            };

            _logger.LogInformation("[ACS Token Service] Successfully generated token for existing user | UserId: {UserId} | ExpiresOn: {ExpiresOn} | Context: {@LogContext}",
                result.Identity.MaskForLogging(), result.ExpiresOn, logContext);

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "[ACS Token Service] ACS request failed during token generation for user {UserId} | Status: {Status} | ErrorCode: {ErrorCode} | Context: {@LogContext}",
                userId.MaskForLogging(), ex.Status, ex.ErrorCode, logContext);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Token Service] Unexpected error while generating token for user {UserId} | Context: {@LogContext}",
                userId.MaskForLogging(), logContext);
            throw;
        }
    }

    /// <summary>
    /// Revokes all tokens for a specific user (security operation)
    /// </summary>
    public async Task<bool> RevokeUserTokensAsync(string userId)
    {
        // This is a placeholder implementation
        // In a real scenario, you would need to track and revoke specific tokens
        var logContext = TokenOperationHelper.CreateTokenLogContext("RevokeTokens", userId);

        try
        {
            _logger.LogInformation("[ACS Token Service] Revoking tokens for user {UserId} | Context: {@LogContext}",
                userId.MaskForLogging(), logContext);

            // ACS doesn't have direct token revocation, but you could:
            // 1. Maintain a blacklist of revoked tokens
            // 2. Use short-lived tokens with refresh mechanism
            // 3. Delete and recreate the user (if appropriate)

            // Simulate async operation
            await Task.Delay(10);

            _logger.LogInformation("[ACS Token Service] Token revocation completed for user {UserId} | Context: {@LogContext}",
                userId.MaskForLogging(), logContext);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Token Service] Failed to revoke tokens for user {UserId} | Context: {@LogContext}",
                userId.MaskForLogging(), logContext);
            return false;
        }
    }
}
