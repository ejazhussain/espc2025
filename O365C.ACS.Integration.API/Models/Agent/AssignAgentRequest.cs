// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Simplified request model for agent assignment to chat threads
/// Following Azure Communication Services best practices
/// </summary>
public class AssignAgentRequest
{
    /// <summary>
    /// The chat thread identifier where the agent should be assigned
    /// </summary>
    [Required(ErrorMessage = "Thread ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Thread ID must be between 1 and 100 characters")]
    public string ThreadId { get; set; } = string.Empty;
}