// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Models.Search;

/// <summary>
/// Represents a document result from Azure AI Search
/// </summary>
public class DocumentResult
{
    public string Id { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string RelatedResource { get; set; } = string.Empty;
    public double Score { get; set; }
}

/// <summary>
/// Represents a knowledge result from Azure AI Search
/// </summary>
public class KnowledgeResult
{
    public string Title { get; set; } = string.Empty;
    public string Chunk { get; set; } = string.Empty;
    public double Score { get; set; }
}