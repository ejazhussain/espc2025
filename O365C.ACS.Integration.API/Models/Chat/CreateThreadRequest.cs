// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Simple request model for creating chat threads
/// Matches the TypeScript implementation for consistency
/// </summary>
public record CreateThreadRequest
{
    /// <summary>
    /// ACS user identifier of the user creating the thread
    /// Must be a valid Communication Services user ID
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Display name of the user creating the thread
    /// Used to generate a personalized topic
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Topic or title for the chat thread
    /// If not provided, will be generated from DisplayName
    /// </summary>
    public string? Topic { get; init; }

    /// <summary>
    /// Timestamp when the request was made
    /// </summary>
    public string? Timestamp { get; init; }
}