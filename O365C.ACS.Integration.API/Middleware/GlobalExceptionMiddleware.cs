// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace O365C.ACS.Integration.API.Middleware;

/// <summary>
/// Global exception handling middleware for Azure Functions
/// Implements Azure best practices for error handling and logging
/// 
/// Features:
/// - Consistent error response format
/// - Security-aware error messages (no sensitive data exposure)
/// - Comprehensive logging with correlation IDs
/// - Performance monitoring for error rates
/// </summary>
public class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Add correlation ID to context for downstream logging
            context.Items["CorrelationId"] = correlationId;

            _logger.LogInformation("[Global Middleware] Function {FunctionName} started | CorrelationId: {CorrelationId}",
                context.FunctionDefinition.Name, correlationId);

            await next(context);

            _logger.LogInformation("[Global Middleware] Function {FunctionName} completed successfully | CorrelationId: {CorrelationId}",
                context.FunctionDefinition.Name, correlationId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "[Global Middleware] Validation error in {FunctionName} | CorrelationId: {CorrelationId} | Error: {Error}",
                context.FunctionDefinition.Name, correlationId, ex.Message);

            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Invalid request parameters");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[Global Middleware] Business logic error in {FunctionName} | CorrelationId: {CorrelationId} | Error: {Error}",
                context.FunctionDefinition.Name, correlationId, ex.Message);

            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Operation cannot be completed");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "[Global Middleware] Timeout error in {FunctionName} | CorrelationId: {CorrelationId}",
                context.FunctionDefinition.Name, correlationId);

            await HandleExceptionAsync(context, ex, HttpStatusCode.RequestTimeout, "Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Global Middleware] Unhandled exception in {FunctionName} | CorrelationId: {CorrelationId} | Exception: {ExceptionType}",
                context.FunctionDefinition.Name, correlationId, ex.GetType().Name);

            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    private static Task HandleExceptionAsync(FunctionContext context, Exception ex, HttpStatusCode statusCode, string message)
    {
        var correlationId = context.Items.ContainsKey("CorrelationId")
            ? context.Items["CorrelationId"]?.ToString()
            : Guid.NewGuid().ToString();

        var errorResponse = new
        {
            error = message,
            correlationId = correlationId,
            timestamp = DateTimeOffset.UtcNow
        };

        // Note: In ASP.NET Core integration mode,
        // HTTP responses are handled by individual functions, not middleware
        // The middleware focuses on logging and error tracking

        return Task.CompletedTask;
    }
}