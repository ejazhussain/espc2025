// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Represents the status of an agent work item
/// </summary>
public enum AgentWorkItemStatus
{
    /// <summary>
    /// Work item is in queue, not yet claimed by any agent
    /// </summary>
    Unassigned = 0,

    /// <summary>
    /// Work item has been claimed by an agent but not yet active
    /// </summary>
    Claimed = 1,

    /// <summary>
    /// Work item is active and awaiting response
    /// </summary>
    Active = 2,

    /// <summary>
    /// Work item has been resolved and closed
    /// </summary>
    Resolved = 3,

    /// <summary>
    /// Work item was cancelled (by agent or customer before being accepted)
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Agent work item model with queue management support
/// Supports atomic assignment to prevent race conditions
/// </summary>
public record AgentWorkItem
{
    /// <summary>
    /// Unique identifier for the work item (thread ID)
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the work item
    /// </summary>
    [JsonProperty("status")]
    public AgentWorkItemStatus Status { get; init; }

    /// <summary>
    /// ACS User ID of the assigned agent (null if unassigned)
    /// </summary>
    [JsonProperty("assignedAgentId")]
    public string? AssignedAgentId { get; init; }

    /// <summary>
    /// Display name of the assigned agent (null if unassigned)
    /// </summary>
    [JsonProperty("assignedAgentName")]
    public string? AssignedAgentName { get; init; }

    /// <summary>
    /// Timestamp when the work item was claimed by an agent (null if unclaimed)
    /// </summary>
    [JsonProperty("claimedAt")]
    public DateTimeOffset? ClaimedAt { get; init; }

    /// <summary>
    /// Customer display name for queue display
    /// </summary>
    [JsonProperty("customerName")]
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>
    /// ACS User ID of the customer who created the thread
    /// Used for thread isolation and security validation
    /// </summary>
    [JsonProperty("creatorUserId")]
    public string? CreatorUserId { get; init; }

    /// <summary>
    /// Partition key for Cosmos DB (derived from Id)
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => Id;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata for the work item (e.g., meeting information)
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Customer ID (ACS User ID) associated with this work item
    /// </summary>
    [JsonProperty("customerId")]
    public string? CustomerId { get; init; }

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    [JsonProperty("lastModifiedAt")]
    public DateTime? LastModifiedAt { get; init; }

    /// <summary>
    /// Calculated wait time in seconds
    /// </summary>
    [JsonIgnore]
    public int WaitTimeSeconds => (int)(DateTimeOffset.UtcNow - CreatedAt).TotalSeconds;

    /// <summary>
    /// Calculated priority based on wait time (high if >5 minutes)
    /// </summary>
    [JsonIgnore]
    public string Priority => WaitTimeSeconds > 300 ? "HIGH" : "NORMAL";
}
