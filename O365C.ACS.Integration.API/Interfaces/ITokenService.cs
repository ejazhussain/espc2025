// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;
using O365C.ACS.Integration.API.Models.Token;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for Azure Communication Services token management
/// Handles user identity and access token operations
/// 
/// Security features:
/// - Secure token generation with proper scoping
/// - Token lifecycle management
/// - Input validation and sanitization
/// - Comprehensive error handling and logging
/// 
/// Reference: https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/identity/access-tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a new ACS user and generates a token with specified scopes
    /// </summary>
    /// <param name="scopes">List of communication token scopes (e.g., chat, voip)</param>
    /// <returns>ACS user token response containing user identity and access token</returns>
    Task<UserTokenResponse> CreateUserAndTokenAsync(IEnumerable<CommunicationTokenScope> scopes);

    /// <summary>
    /// Generates a new token for an existing ACS user
    /// </summary>
    /// <param name="userId">Existing ACS user identifier</param>
    /// <param name="scopes">List of communication token scopes</param>
    /// <returns>ACS user token response</returns>
    Task<UserTokenResponse> GetTokenForUserAsync(string userId, IEnumerable<CommunicationTokenScope> scopes);

    /// <summary>
    /// Revokes all tokens for a specific user (security operation)
    /// </summary>
    /// <param name="userId">ACS user identifier</param>
    /// <returns>True if revocation successful, false otherwise</returns>
    Task<bool> RevokeUserTokensAsync(string userId);
}
