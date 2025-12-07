// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Models.Email;
using O365C.ACS.Integration.API.Models.Settings;
using System.Text;

namespace O365C.ACS.Integration.API.Services;

public class EmailService : IEmailService
{
    private readonly IGraphApiService _graphApiService;
    private readonly ILogger<EmailService> _logger;
    private readonly AppSettings _appSettings;

    public EmailService(IGraphApiService graphApiService, ILogger<EmailService> logger, AppSettings appSettings)
    {
        _graphApiService = graphApiService ?? throw new ArgumentNullException(nameof(graphApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public async Task<SendTranscriptEmailResponse> SendTranscriptEmailAsync(SendTranscriptEmailRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new ArgumentException("Customer email is required.", nameof(request.CustomerEmail));
        }

        _logger.LogInformation("[Email Service] Sending transcript email to {CustomerEmail} for thread {ThreadId}", 
            request.CustomerEmail, request.ThreadId);

        try
        {
            // Generate email content
            var emailSubject = $"Support Case Summary - {request.ThreadId}";
            var emailBody = GenerateEmailBody(request);

            // Send email using Microsoft Graph API
            await _graphApiService.SendEmailAsync(request.CustomerEmail, emailSubject, emailBody, _appSettings.EmailSettings.ServiceAccountEmail);

            var response = new SendTranscriptEmailResponse
            {
                Success = true,
                Message = "Transcript email sent successfully.",
                EmailId = Guid.NewGuid().ToString()
            };

            _logger.LogInformation("[Email Service] Successfully sent transcript email to {CustomerEmail}", request.CustomerEmail);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email Service] Failed to send transcript email to {CustomerEmail}", request.CustomerEmail);
            
            return new SendTranscriptEmailResponse
            {
                Success = false,
                Message = $"Failed to send email: {ex.Message}",
                EmailId = string.Empty
            };
        }
    }

    private string GenerateEmailBody(SendTranscriptEmailRequest request)
    {
        var htmlBody = new StringBuilder();
        
        htmlBody.AppendLine("<!DOCTYPE html>");
        htmlBody.AppendLine("<html>");
        htmlBody.AppendLine("<head>");
        htmlBody.AppendLine("    <meta charset='utf-8'>");
        htmlBody.AppendLine("    <style>");
        htmlBody.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; background-color: #f5f5f5; }");
        htmlBody.AppendLine("        .container { background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); overflow: hidden; }");
        htmlBody.AppendLine("        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }");
        htmlBody.AppendLine("        .header h2 { margin: 0; font-size: 28px; font-weight: 300; }");
        htmlBody.AppendLine("        .content { padding: 30px; }");
        htmlBody.AppendLine("        .case-info { background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 30px; }");
        htmlBody.AppendLine("        .case-info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }");
        htmlBody.AppendLine("        .case-info-item { margin: 5px 0; }");
        htmlBody.AppendLine("        .case-info-label { font-weight: 600; color: #495057; }");
        htmlBody.AppendLine("        .section { margin-bottom: 25px; padding: 25px; border-radius: 8px; background-color: #ffffff; border: 1px solid #e9ecef; }");
        htmlBody.AppendLine("        .section-title { color: #495057; font-weight: 600; font-size: 18px; margin-bottom: 15px; display: flex; align-items: center; }");
        htmlBody.AppendLine("        .section-title .icon { margin-right: 10px; font-size: 20px; }");
        htmlBody.AppendLine("        .problem { border-left: 5px solid #dc3545; background: linear-gradient(90deg, rgba(220, 53, 69, 0.05) 0%, rgba(255, 255, 255, 0) 100%); }");
        htmlBody.AppendLine("        .solution { border-left: 5px solid #28a745; background: linear-gradient(90deg, rgba(40, 167, 69, 0.05) 0%, rgba(255, 255, 255, 0) 100%); }");
        htmlBody.AppendLine("        .summary { border-left: 5px solid #007bff; background: linear-gradient(90deg, rgba(0, 123, 255, 0.05) 0%, rgba(255, 255, 255, 0) 100%); }");
        htmlBody.AppendLine("        .section-content { font-size: 16px; line-height: 1.7; }");
        htmlBody.AppendLine("        .footer { background-color: #f8f9fa; padding: 25px; text-align: center; color: #6c757d; border-top: 1px solid #e9ecef; }");
        htmlBody.AppendLine("        .footer p { margin: 10px 0; }");
        htmlBody.AppendLine("        .signature { font-weight: 600; color: #495057; }");
        htmlBody.AppendLine("    </style>");
        htmlBody.AppendLine("</head>");
        htmlBody.AppendLine("<body>");
        htmlBody.AppendLine("    <div class='container'>");
        
        // Header
        htmlBody.AppendLine("        <div class='header'>");
        htmlBody.AppendLine("            <h2>Support Case Summary</h2>");
        htmlBody.AppendLine("        </div>");

        // Content
        htmlBody.AppendLine("        <div class='content'>");

        // Case Information
        htmlBody.AppendLine("            <div class='case-info'>");
        htmlBody.AppendLine("                <div class='case-info-grid'>");
        htmlBody.AppendLine("                    <div class='case-info-item'>");
        htmlBody.AppendLine($"                        <span class='case-info-label'>Customer:</span> {request.CustomerName}");
        htmlBody.AppendLine("                    </div>");
        htmlBody.AppendLine("                    <div class='case-info-item'>");
        htmlBody.AppendLine($"                        <span class='case-info-label'>Case ID:</span> {request.ThreadId}");
        htmlBody.AppendLine("                    </div>");
        htmlBody.AppendLine("                    <div class='case-info-item'>");
        htmlBody.AppendLine($"                        <span class='case-info-label'>Resolution Date:</span> {request.ResolutionDate}");
        htmlBody.AppendLine("                    </div>");
        htmlBody.AppendLine("                    <div class='case-info-item'>");
        htmlBody.AppendLine($"                        <span class='case-info-label'>Support Agent:</span> {request.AgentName}");
        htmlBody.AppendLine("                    </div>");
        htmlBody.AppendLine("                </div>");
        htmlBody.AppendLine("            </div>");

        // Problem Reported
        htmlBody.AppendLine("            <div class='section problem'>");
        htmlBody.AppendLine("                <div class='section-title'>");
        htmlBody.AppendLine("                    <span class='icon'>🔴</span> Problem Reported");
        htmlBody.AppendLine("                </div>");
        htmlBody.AppendLine($"                <div class='section-content'>{request.ProblemReported}</div>");
        htmlBody.AppendLine("            </div>");

        // Solution Provided
        htmlBody.AppendLine("            <div class='section solution'>");
        htmlBody.AppendLine("                <div class='section-title'>");
        htmlBody.AppendLine("                    <span class='icon'>✅</span> Solution Provided");
        htmlBody.AppendLine("                </div>");
        htmlBody.AppendLine($"                <div class='section-content'>{request.SolutionProvided}</div>");
        htmlBody.AppendLine("            </div>");

        // Summary
        htmlBody.AppendLine("            <div class='section summary'>");
        htmlBody.AppendLine("                <div class='section-title'>");
        htmlBody.AppendLine("                    <span class='icon'>📋</span> Summary");
        htmlBody.AppendLine("                </div>");
        htmlBody.AppendLine($"                <div class='section-content'>{request.Summary}</div>");
        htmlBody.AppendLine("            </div>");

        htmlBody.AppendLine("        </div>");

        // Footer
        htmlBody.AppendLine("        <div class='footer'>");
        htmlBody.AppendLine("            <p>Thank you for contacting our support team. If you have any further questions, please don't hesitate to reach out.</p>");
        htmlBody.AppendLine("            <p class='signature'>Best regards,<br><strong>Customer Support Team</strong></p>");
        htmlBody.AppendLine("        </div>");
        
        htmlBody.AppendLine("    </div>");
        
        htmlBody.AppendLine("</body>");
        htmlBody.AppendLine("</html>");

        return htmlBody.ToString();
    }


}
