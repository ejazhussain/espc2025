// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Settings;

namespace O365C.ACS.Integration.API.Services;

public class GraphApiService : IGraphApiService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<GraphApiService> _logger;
    private readonly Models.Settings.EmailSettings _emailSettings;

    public GraphApiService(GraphServiceClient graphServiceClient, ILogger<GraphApiService> logger, AppSettings appSettings)
    {
        _graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailSettings = appSettings?.EmailSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("To email address is required.", nameof(toEmail));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Email subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException("Email body is required.", nameof(htmlBody));
        }

        _logger.LogInformation("[Graph API Service] Sending email to {ToEmail} with subject '{Subject}'", toEmail, subject);

        try
        {
            var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = htmlBody,
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = toEmail,
                            },
                        },
                    },
                },
                SaveToSentItems = true
            };

            // Use a specific user account instead of /me for application permissions
            // If fromEmail is provided, use it; otherwise use the configured service account
            var senderEmail = fromEmail ?? _emailSettings.ServiceAccountEmail;
            
            _logger.LogInformation("[Graph API Service] Sending email from {FromEmail} to {ToEmail}", senderEmail, toEmail);
            
            // Use /users/{userPrincipalName}/sendMail instead of /me/sendMail
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(requestBody);

            _logger.LogInformation("[Graph API Service] Successfully sent email from {FromEmail} to {ToEmail}", senderEmail, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Graph API Service] Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        _logger.LogDebug("[Graph API Service] Getting user {UserId}", userId);

        try
        {
            var user = await _graphServiceClient.Users[userId].GetAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Graph API Service] Failed to get user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string? filter = null, int top = 50)
    {
        _logger.LogDebug("[Graph API Service] Getting users with filter '{Filter}', top {Top}", filter ?? "none", top);

        try
        {
            var users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Top = top;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    requestConfiguration.QueryParameters.Filter = filter;
                }
            });

            return users?.Value ?? new List<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Graph API Service] Failed to get users with filter '{Filter}'", filter ?? "none");
            throw;
        }
    }
}