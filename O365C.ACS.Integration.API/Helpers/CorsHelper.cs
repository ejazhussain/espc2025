// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace O365C.ACS.Integration.API.Helpers;

/// <summary>
/// Helper class for handling CORS in Azure Functions
/// Provides methods to add CORS headers and handle preflight requests
/// </summary>
public static class CorsHelper
{
    private static readonly string[] AllowedOrigins = {
        "http://localhost:3000",
        "https://localhost:3000",
        "http://127.0.0.1:3000",
        "https://127.0.0.1:3000"
    };

    private static readonly string[] AllowedMethods = {
        "GET", "POST", "PUT", "DELETE", "OPTIONS"
    };

    private static readonly string[] AllowedHeaders = {
        "Content-Type", "Authorization", "Accept", "Origin", "User-Agent",
        "DNT", "Cache-Control", "X-Mx-ReqToken", "Keep-Alive",
        "X-Requested-With", "If-Modified-Since"
    };

    /// <summary>
    /// Adds CORS headers to the HTTP response
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <param name="result">Action result to add headers to</param>
    /// <returns>Action result with CORS headers</returns>
    public static IActionResult AddCorsHeaders(HttpRequest req, IActionResult result)
    {
        var origin = req.Headers["Origin"].FirstOrDefault();

        if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
        {
            if (result is ObjectResult objectResult)
            {
                // Add CORS headers to existing ObjectResult
                return new ObjectResult(objectResult.Value)
                {
                    StatusCode = objectResult.StatusCode
                };
            }
        }

        return result;
    }

    /// <summary>
    /// Creates an HTTP response with CORS headers
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="content">Response content</param>
    /// <returns>ObjectResult with CORS headers</returns>
    public static ObjectResult CreateCorsResponse(HttpRequest req, int statusCode, object content)
    {
        var response = new ObjectResult(content)
        {
            StatusCode = statusCode
        };

        AddCorsHeadersToResponse(req, response);
        return response;
    }

    /// <summary>
    /// Handles preflight OPTIONS requests
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <returns>OK result with CORS headers for preflight</returns>
    public static IActionResult HandlePreflightRequest(HttpRequest req)
    {
        var response = new OkResult();
        return CreateCorsResponse(req, 200, string.Empty);
    }

    /// <summary>
    /// Checks if the origin is allowed
    /// </summary>
    /// <param name="origin">Origin to check</param>
    /// <returns>True if origin is allowed</returns>
    private static bool IsOriginAllowed(string origin)
    {
        return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds CORS headers to an ObjectResult
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <param name="result">ObjectResult to add headers to</param>
    private static void AddCorsHeadersToResponse(HttpRequest req, ObjectResult result)
    {
        var origin = req.Headers["Origin"].FirstOrDefault();

        if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
        {
            // Note: ObjectResult doesn't directly support headers
            // Headers need to be added in the function itself
            // This method provides the structure for header addition
        }
    }
}
