// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Represents an agent user in the support system
/// Contains agent profile information, skills, and availability status
/// 
/// This model supports:
/// - Agent identification and contact information
/// </summary>
public record AgentUser
{   

    /// <summary>
    /// Azure Communication Services user identifier
    /// Used for chat operations and thread management
    /// </summary>
    [JsonPropertyName("acsUserId")]
    public string AcsUserId { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the agent
    /// Shown to customers in chat interfaces
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Agent's email address
    /// Used for notifications and contact
    /// </summary>
    [JsonPropertyName("teamsUserId")]
    public string TeamsUserId { get; init; } = string.Empty;


}
