// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Agent;

/// <summary>
/// Request model for creating an agent work item
/// </summary>
public class CreateAgentWorkItemRequest
{
    public string Id { get; set; } = string.Empty;
    public AgentWorkItemStatus Status { get; set; }
}

/// <summary>
/// Request model for updating an agent work item
/// </summary>
public class UpdateAgentWorkItemRequest
{
    public AgentWorkItemStatus Status { get; set; }
}
