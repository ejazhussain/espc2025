// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using O365C.ACS.Integration.API.Models.Email;

namespace O365C.ACS.Integration.API.Interfaces;

public interface IEmailService
{
    Task<SendTranscriptEmailResponse> SendTranscriptEmailAsync(SendTranscriptEmailRequest request);
}
