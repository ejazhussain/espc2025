// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Chat;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Interface for Azure Communication Services chat operations
/// Simplified interface for chat thread management, user participation, and messaging
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Creates a new chat thread with specified topic and participants
    /// </summary>
    /// <param name="request">Thread creation request with details</param>
    /// <returns>Thread creation response with thread identifier</returns>
    Task<CreateThreadResponse> CreateThreadAsync(CreateThreadRequest request);

    /// <summary>
    /// Adds a user to an existing chat thread
    /// </summary>
    /// <param name="request">Join thread request with user and thread details</param>
    /// <returns>Join operation response with success status</returns>
    Task<JoinThreadResponse> JoinThreadAsync(JoinThreadRequest request);

    /// <summary>
    /// Sends a message to a chat thread
    /// </summary>
    /// <param name="request">Message send request with content and metadata</param>
    /// <returns>Send operation response with message identifier</returns>
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request);

    /// <summary>
    /// Sends conversation history from AI chat to ACS chat thread
    /// </summary>
    /// <param name="request">Conversation history request with messages from AI chat</param>
    /// <returns>Send operation response with success status and message count</returns>
    Task<SendConversationHistoryResponse> SendConversationHistoryAsync(SendConversationHistoryRequest request);

    /// <summary>
    /// Removes a user from a chat thread
    /// </summary>
    /// <param name="threadId">Chat thread identifier</param>
    /// <param name="userId">User identifier to remove</param>
    /// <returns>Success status of remove operation</returns>
    Task<bool> RemoveUserFromThreadAsync(string threadId, string userId);

    /// <summary>
    /// Gets thread information and metadata
    /// </summary>
    /// <param name="threadId">Chat thread identifier</param>
    /// <returns>Thread information or null if not found</returns>
    Task<CreateThreadResponse?> GetThreadInfoAsync(string threadId);

    /// <summary>
    /// Gets or creates a persistent admin user for ACS operations
    /// This ensures consistent admin user management across all services
    /// </summary>
    /// <returns>Tuple containing admin user ID and access token</returns>
    Task<(string userId, string accessToken)> GetOrCreateAdminUserAsync();

    /// <summary>
    /// Deletes a chat thread and all associated data
    /// This includes the ACS thread, messages, and CosmosDB records
    /// </summary>
    /// <param name="threadId">Chat thread identifier to delete</param>
    /// <returns>Success status of delete operation</returns>
    Task<bool> DeleteThreadAsync(string threadId);

    /// <summary>
    /// Retrieves chat messages for a specific thread ordered by send time.
    /// </summary>
    /// <param name="threadId">Chat thread identifier</param>
    /// <returns>Collection of chat messages</returns>
    Task<IReadOnlyList<ChatThreadMessage>> GetThreadMessagesAsync(string threadId);
}