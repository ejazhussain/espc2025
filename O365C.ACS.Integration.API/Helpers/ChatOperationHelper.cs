// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using O365C.ACS.Integration.API.Extensions;

namespace O365C.ACS.Integration.API.Helpers;

/// <summary>
/// Helper class for chat-specific operations and utilities
/// Provides specialized functionality for ACS chat management, thread lifecycle, and messaging
/// 
/// Features:
/// - Thread management with enterprise-grade policies
/// - Message validation and content filtering
/// - User participation tracking and management
/// - Chat analytics and performance monitoring
/// - Compliance and audit trail management
/// 
/// Reference: https://docs.microsoft.com/en-us/azure/communication-services/concepts/chat/concepts
/// </summary>
public static class ChatOperationHelper
{
    /// <summary>
    /// Maximum allowed lengths for chat content (following ACS limits and enterprise policies)
    /// </summary>
    public static class ContentLimits
    {
        public const int MaxTopicLength = 255;
        public const int MaxMessageLength = 8000;
        public const int MaxDisplayNameLength = 100;
        public const int MaxThreadParticipants = 250;
        public const int MaxCustomMetadataKeys = 10;
        public const int MaxCustomMetadataValueLength = 500;
    }

    /// <summary>
    /// Supported chat thread priorities for enterprise routing
    /// </summary>
    public static class ThreadPriorities
    {
        public const string Low = "low";
        public const string Normal = "normal";
        public const string High = "high";
        public const string Critical = "critical";

        public static readonly string[] All = { Low, Normal, High, Critical };

        public static bool IsValid(string priority) =>
            All.Contains(priority, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a sanitized topic string for chat threads
    /// Ensures topic meets ACS requirements and enterprise content policies
    /// </summary>
    /// <param name="displayName">User display name for context</param>
    /// <param name="customTopic">Optional custom topic</param>
    /// <param name="department">Department for topic categorization</param>
    /// <returns>Sanitized and formatted topic string</returns>
    public static string CreateChatThreadTopic(string displayName, string? customTopic = null, string? department = null)
    {
        // Validate inputs
        displayName = displayName.ValidateNotNullOrEmpty(nameof(displayName));

        // Generate topic based on context
        string topic;
        if (!string.IsNullOrWhiteSpace(customTopic))
        {
            topic = customTopic.Sanitize();
        }
        else
        {
            var deptPrefix = !string.IsNullOrWhiteSpace(department) ? $"{department} - " : "";
            topic = $"{deptPrefix}Support chat for {displayName.Sanitize()}";
        }

        // Ensure topic meets length requirements
        if (topic.Length > ContentLimits.MaxTopicLength)
        {
            topic = topic.Substring(0, ContentLimits.MaxTopicLength - 3) + "...";
        }

        return topic;
    }

    /// <summary>
    /// Generates a unique thread identifier with enterprise-friendly format
    /// Includes timestamp and department context for better tracking
    /// </summary>
    /// <param name="department">Department for thread categorization</param>
    /// <param name="priority">Thread priority level</param>
    /// <returns>Unique, trackable thread identifier</returns>
    public static string GenerateThreadId(string department = "support", string priority = ThreadPriorities.Normal)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Short GUID for readability
        var deptCode = GetDepartmentCode(department);
        var priorityCode = GetPriorityCode(priority);

        return $"thread_{deptCode}_{priorityCode}_{timestamp}_{uniqueId}";
    }

    /// <summary>
    /// Validates and sanitizes message content for enterprise compliance
    /// Applies content filtering, length validation, and security checks
    /// </summary>
    /// <param name="messageContent">Raw message content</param>
    /// <param name="allowHtml">Whether to allow basic HTML formatting</param>
    /// <returns>Validation result with sanitized content and any warnings</returns>
    public static MessageValidationResult ValidateMessageContent(string messageContent, bool allowHtml = false)
    {
        var result = new MessageValidationResult
        {
            OriginalContent = messageContent,
            IsValid = true,
            Warnings = new List<string>()
        };

        // Basic validation
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            result.IsValid = false;
            result.ErrorMessage = "Message content cannot be empty";
            result.SanitizedContent = string.Empty;
            return result;
        }

        // Length validation
        if (messageContent.Length > ContentLimits.MaxMessageLength)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Message exceeds maximum length of {ContentLimits.MaxMessageLength} characters";
            result.SanitizedContent = messageContent.Substring(0, ContentLimits.MaxMessageLength);
            return result;
        }

        // Content sanitization
        result.SanitizedContent = messageContent.Sanitize(allowHtml);

        // Check for potential issues
        if (result.SanitizedContent.Length != messageContent.Length)
        {
            result.Warnings.Add("Message content was sanitized - some characters were removed");
        }

        // Basic content analysis (can be expanded for enterprise content policies)
        if (ContainsSensitiveInformation(result.SanitizedContent))
        {
            result.Warnings.Add("Message may contain sensitive information - review before sending");
        }

        return result;
    }

    /// <summary>
    /// Creates structured log context for chat operations
    /// Provides consistent logging across all chat-related operations
    /// </summary>
    /// <param name="operation">Chat operation name</param>
    /// <param name="threadId">Optional thread ID (will be masked)</param>
    /// <param name="userId">Optional user ID (will be masked)</param>
    /// <param name="additionalContext">Additional context data</param>
    /// <returns>Structured log context for consistent logging</returns>
    public static Dictionary<string, object> CreateChatLogContext(
        string operation,
        string? threadId = null,
        string? userId = null,
        Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["OperationId"] = GenerateChatOperationId(operation),
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["Component"] = "ChatManager"
        };

        if (!string.IsNullOrEmpty(threadId))
        {
            context["ThreadId"] = threadId.MaskForLogging();
        }

        if (!string.IsNullOrEmpty(userId))
        {
            context["UserId"] = userId.MaskForLogging();
        }

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[$"Context.{kvp.Key}"] = kvp.Value;
            }
        }

        return context;
    }

    /// <summary>
    /// Estimates response time for agent assignment based on current workload
    /// Uses department statistics and priority to provide realistic estimates
    /// </summary>
    /// <param name="department">Department for agent assignment</param>
    /// <param name="priority">Request priority level</param>
    /// <returns>Estimated response time</returns>
    public static TimeSpan EstimateAgentResponseTime(string department, string priority)
    {
        // Base response times by department (in minutes)
        var baseTimes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "IT Support", 5 },
            { "Sales", 3 },
            { "Billing", 10 },
            { "Technical", 15 },
            { "General", 8 }
        };

        var baseMinutes = baseTimes.GetValueOrDefault(department, 8);

        // Priority multipliers
        var priorityMultiplier = priority.ToLowerInvariant() switch
        {
            ThreadPriorities.Critical => 0.2,
            ThreadPriorities.High => 0.5,
            ThreadPriorities.Normal => 1.0,
            ThreadPriorities.Low => 1.5,
            _ => 1.0
        };

        var estimatedMinutes = (int)(baseMinutes * priorityMultiplier);
        return TimeSpan.FromMinutes(Math.Max(1, estimatedMinutes));
    }

    /// <summary>
    /// Generates department code for thread identification
    /// </summary>
    private static string GetDepartmentCode(string department)
    {
        return department.ToLowerInvariant() switch
        {
            "it support" => "its",
            "sales" => "sal",
            "billing" => "bil",
            "technical" => "tec",
            "general" => "gen",
            _ => "sup" // Default support code
        };
    }

    /// <summary>
    /// Generates priority code for thread identification
    /// </summary>
    private static string GetPriorityCode(string priority)
    {
        return priority.ToLowerInvariant() switch
        {
            ThreadPriorities.Critical => "crt",
            ThreadPriorities.High => "hgh",
            ThreadPriorities.Normal => "nrm",
            ThreadPriorities.Low => "low",
            _ => "nrm"
        };
    }

    /// <summary>
    /// Generates unique operation ID for chat operations
    /// </summary>
    private static string GenerateChatOperationId(string operation)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var operationId = Guid.NewGuid().ToString("N")[..8];
        return $"chat_{operation}_{timestamp}_{operationId}";
    }

    /// <summary>
    /// Basic check for sensitive information in message content
    /// Can be expanded with enterprise-specific policies and ML models
    /// </summary>
    private static bool ContainsSensitiveInformation(string content)
    {
        var sensitivePatterns = new[]
        {
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN pattern
            @"\b\d{16}\b", // Credit card pattern
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Email pattern
        };

        return sensitivePatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(content, pattern));
    }
}

/// <summary>
/// Result of message content validation with sanitized content and warnings
/// </summary>
public record MessageValidationResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string SanitizedContent { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}