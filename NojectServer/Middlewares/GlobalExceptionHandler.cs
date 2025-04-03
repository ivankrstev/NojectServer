using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NojectServer.Middlewares;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception
        _logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);
        // Create a ProblemDetails object for the response
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
        };

        // In development, include more specific error details
        if (httpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

            // For validation exceptions, include the validation errors
            if (exception is Microsoft.Extensions.Options.OptionsValidationException validationEx)
            {
                problemDetails.Extensions["errors"] = validationEx.Failures;
            }
        }
        else
        {
            // In production, use a generic message
            problemDetails.Detail = "An error occurred. Please try again later.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
