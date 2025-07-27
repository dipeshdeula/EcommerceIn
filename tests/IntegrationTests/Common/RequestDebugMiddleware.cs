using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Common;

public class RequestDebugMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDebugMiddleware> _logger;

    public RequestDebugMiddleware(RequestDelegate next, ILogger<RequestDebugMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log detailed information about the request
        _logger.LogInformation(
            "Request: {Method} {Path}{QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);
        
        // Log request headers for debugging authentication
        foreach (var header in context.Request.Headers)
        {
            _logger.LogDebug("Header: {Key}: {Value}", header.Key, header.Value);
        }
        
        // Capture the body content
        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
                
            requestBody = await reader.ReadToEndAsync();
            _logger.LogDebug("Request body: {Body}", requestBody);
            
            // Reset the position to allow reading again in the action
            context.Request.Body.Position = 0;
        }

        // Execute the request
        await _next(context);

        // Log response details
        _logger.LogInformation(
            "Response: {StatusCode} for {Method} {Path}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path);
    }
}