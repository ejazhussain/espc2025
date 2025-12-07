// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace O365C.ACS.Integration.API.Models.Admin;

/// <summary>
/// Represents an admin user for Azure Communication Services operations
/// This admin user is stored in Cosmos DB and reused across all chat operations
/// to avoid permission issues when managing chat threads
/// </summary>
public record AdminUser
{
    /// <summary>
    /// Unique identifier for the admin user record (typically "system-admin")
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Azure Communication Services user ID
    /// </summary>
    [JsonPropertyName("acsUserId")]
    public required string AcsUserId { get; init; }

    /// <summary>
    /// Display name for the admin user
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// When this admin user was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last time this admin user was used/updated
    /// </summary>
    [JsonPropertyName("lastUsedAt")]
    public DateTimeOffset LastUsedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Partition key for Cosmos DB (using "environment" to match existing container)
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; init; } = "admin";

    /// <summary>
    /// Whether this admin user is currently active
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; } = true;
}