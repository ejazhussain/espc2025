// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace O365C.ACS.Integration.API.Extensions;

/// <summary>
/// Extension methods for string validation and security operations
/// Provides helper methods for input validation, sanitization, and security-aware processing
/// 
/// These extensions follow security best practices for:
/// - Input validation and sanitization
/// - PII protection and data masking
/// - Safe string operations with proper error handling
/// - Enterprise-grade validation rules
/// </summary>
public static class StringValidationExtensions
{
    /// <summary>
    /// Validates that a string parameter is not null, empty, or whitespace
    /// Throws appropriate exception with parameter name for better debugging
    /// </summary>
    /// <param name="value">String value to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <returns>Trimmed string value</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace</exception>
    public static string ValidateNotNullOrEmpty(this string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} cannot be null, empty, or whitespace", parameterName);
        }

        return value.Trim();
    }

    /// <summary>
    /// Validates string length within specified bounds
    /// </summary>
    /// <param name="value">String value to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <param name="minLength">Minimum allowed length</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>Validated string value</returns>
    /// <exception cref="ArgumentException">Thrown when length is outside bounds</exception>
    public static string ValidateLength(this string value, string parameterName, int minLength = 1, int maxLength = int.MaxValue)
    {
        value = value.ValidateNotNullOrEmpty(parameterName);

        if (value.Length < minLength)
        {
            throw new ArgumentException($"{parameterName} must be at least {minLength} characters long", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} must not exceed {maxLength} characters", parameterName);
        }

        return value;
    }

    /// <summary>
    /// Formats ACS user ID for logging (masks sensitive parts)
    /// Helps with debugging while maintaining security and privacy
    /// </summary>
    /// <param name="userId">ACS user identifier</param>
    /// <returns>Masked user ID safe for logging</returns>
    public static string MaskForLogging(this string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "[empty]";
        }

        if (userId.Length <= 8)
        {
            return "***";
        }

        // Show first 4 and last 4 characters, mask the middle
        var start = userId.Substring(0, 4);
        var end = userId.Substring(userId.Length - 4);
        var maskedLength = userId.Length - 8;
        
        return $"{start}{'*'.ToString().PadLeft(Math.Min(maskedLength, 6), '*')}{end}";
    }

    /// <summary>
    /// Sanitizes input string for safe display and processing
    /// Removes potentially dangerous characters and normalizes content
    /// </summary>
    /// <param name="input">Input string to sanitize</param>
    /// <param name="allowHtml">Whether to allow basic HTML tags</param>
    /// <returns>Sanitized string safe for processing</returns>
    public static string Sanitize(this string input, bool allowHtml = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sanitized = input.Trim();

        if (!allowHtml)
        {
            // Remove HTML tags and potentially dangerous characters
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"<[^>]*>", "");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[<>\""]", "");
        }

        // Remove control characters except common whitespace
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        // Normalize whitespace
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }

    /// <summary>
    /// Validates email address format
    /// Uses enterprise-grade email validation
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <returns>Normalized email address</returns>
    /// <exception cref="ArgumentException">Thrown when email format is invalid</exception>
    public static string ValidateEmail(this string email, string parameterName = "email")
    {
        email = email.ValidateNotNullOrEmpty(parameterName);

        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Invalid email format: {email}", parameterName);
        }
    }

    /// <summary>
    /// Validates that string contains only alphanumeric characters and allowed symbols
    /// Useful for usernames, IDs, and other identifier validation
    /// </summary>
    /// <param name="value">String to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <param name="allowedSymbols">Additional allowed symbols (default: hyphen, underscore)</param>
    /// <returns>Validated string</returns>
    /// <exception cref="ArgumentException">Thrown when string contains invalid characters</exception>
    public static string ValidateAlphanumeric(this string value, string parameterName, string allowedSymbols = "-_")
    {
        value = value.ValidateNotNullOrEmpty(parameterName);

        var allowedPattern = $@"^[a-zA-Z0-9{System.Text.RegularExpressions.Regex.Escape(allowedSymbols)}]+$";
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, allowedPattern))
        {
            throw new ArgumentException($"{parameterName} can only contain letters, numbers, and these symbols: {allowedSymbols}", parameterName);
        }

        return value;
    }

    /// <summary>
    /// Extracts and validates thread ID from various input formats
    /// Handles different ACS thread ID formats and validates structure
    /// </summary>
    /// <param name="threadId">Thread identifier to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <returns>Validated and normalized thread ID</returns>
    /// <exception cref="ArgumentException">Thrown when thread ID format is invalid</exception>
    public static string ValidateThreadId(this string threadId, string parameterName = "threadId")
    {
        threadId = threadId.ValidateNotNullOrEmpty(parameterName);

        // ACS thread IDs typically follow specific patterns
        // This validation can be enhanced based on actual ACS thread ID formats
        if (threadId.Length < 10 || threadId.Length > 100)
        {
            throw new ArgumentException($"Invalid thread ID format: {threadId}", parameterName);
        }

        return threadId;
    }
}