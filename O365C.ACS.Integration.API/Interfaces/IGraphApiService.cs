// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models;

namespace O365C.ACS.Integration.API.Interfaces;

public interface IGraphApiService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null);
    Task<User?> GetUserAsync(string userId);
    Task<IEnumerable<User>> GetUsersAsync(string? filter = null, int top = 50);
}