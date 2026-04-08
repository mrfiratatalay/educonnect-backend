using System.Net.Mail;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred while processing the request.");

        var statusCode = exception switch
        {
            SmtpException => StatusCodes.Status503ServiceUnavailable,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception switch
            {
                SmtpException => "Email Delivery Failed",
                UnauthorizedAccessException => "Unauthorized",
                KeyNotFoundException => "Not Found",
                InvalidOperationException => "Invalid Operation",
                _ => "Server Error"
            },
            Detail = exception switch
            {
                SmtpException => "Dogrulama e-postasi gonderilemedi. Mail servisini kontrol edip tekrar deneyin.",
                _ when statusCode == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
                _ => exception.Message
            }
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
