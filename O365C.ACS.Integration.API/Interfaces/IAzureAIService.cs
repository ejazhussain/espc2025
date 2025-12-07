// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.AI;
using O365C.ACS.Integration.API.Models.Chat;

namespace O365C.ACS.Integration.API.Interfaces;

/// <summary>
/// Provides Azure OpenAI powered capabilities.
/// </summary>
public interface IAzureAIService
{
    Task<ChatTranscriptResponse> GenerateTranscriptAsync(GenerateTranscriptRequest request, IReadOnlyList<ChatThreadMessage> messages);
}
