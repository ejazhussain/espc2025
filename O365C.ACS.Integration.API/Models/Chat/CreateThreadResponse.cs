// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Chat;

/// <summary>
/// Simple response model for thread creation operations
/// Matches the TypeScript implementation for consistency
/// </summary>
public record CreateThreadResponse
{
    /// <summary>
    /// Unique identifier for the created chat thread
    /// Used for all subsequent thread operations
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if the thread creation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}