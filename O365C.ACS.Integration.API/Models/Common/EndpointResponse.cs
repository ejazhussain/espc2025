// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Common;

/// <summary>
/// Response model for endpoint URL requests
/// Provides ACS service endpoint information for client initialization
/// 
/// This model ensures consistent endpoint delivery across all operations
/// while supporting multiple endpoint scenarios and validation requirements.
/// </summary>
public record EndpointResponse
{
    /// <summary>
    /// ACS service endpoint URL
    /// Used for initializing communication clients
    /// Format: https://your-resource.communication.azure.com
    /// </summary>
    public string EndpointUrl { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if endpoint is valid and accessible
    /// </summary>
    public bool IsValid { get; init; } = true;

    /// <summary>
    /// Region information for the endpoint
    /// Useful for latency optimization and compliance
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Service capabilities available at this endpoint
    /// Examples: ["chat", "sms", "calling", "video"]
    /// </summary>
    public string[]? Capabilities { get; init; }

    /// <summary>
    /// Endpoint validation timestamp
    /// Last time the endpoint was verified as accessible
    /// </summary>
    public DateTimeOffset? ValidatedAt { get; init; }

    /// <summary>
    /// Optional error message if endpoint validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}