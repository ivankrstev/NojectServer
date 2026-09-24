using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Creates the common Problem Details responses returned by the API.
/// </summary>
internal static class ApiErrorResponseFactory
{
    private const string ProblemJsonMediaType =
        "application/problem+json";

    /// <summary>
    /// Creates a Problem Details response for a known application failure.
    /// </summary>
    public static ObjectResult CreateFailure(
        HttpContext httpContext,
        ErrorDetails error)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);

        ProblemDetails problemDetails =
            GetProblemDetailsFactory(httpContext).CreateProblemDetails(
                httpContext,
                statusCode: error.StatusCode,
                detail: error.Message,
                instance: GetInstance(httpContext));

        AddApplicationMetadata(
            problemDetails,
            httpContext,
            error.Error);

        return CreateResult(problemDetails);
    }

    /// <summary>
    /// Creates a validation response from application validation errors.
    /// </summary>
    public static ObjectResult CreateValidation(
        HttpContext httpContext,
        ErrorDetails error,
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(validationErrors);

        var modelState = new ModelStateDictionary();

        foreach ((string field, string[] messages) in validationErrors)
        {
            foreach (string message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }

        return CreateValidation(
            httpContext,
            error,
            modelState);
    }

    /// <summary>
    /// Creates the same validation response for MVC model-validation errors.
    /// </summary>
    public static ObjectResult CreateValidation(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(modelState);

        return CreateValidation(
            httpContext,
            ValidationFailureData.Error,
            modelState);
    }

    // Creates a validation response from the given error details and model state.
    private static ObjectResult CreateValidation(
        HttpContext httpContext,
        ErrorDetails error,
        ModelStateDictionary modelState)
    {
        ValidationProblemDetails problemDetails =
            GetProblemDetailsFactory(httpContext)
                .CreateValidationProblemDetails(
                    httpContext,
                    modelState,
                    statusCode: error.StatusCode,
                    title: error.Message,
                    instance: GetInstance(httpContext));

        AddApplicationMetadata(
            problemDetails,
            httpContext,
            error.Error);

        return CreateResult(problemDetails);
    }

    // Gets the ProblemDetailsFactory from the request services.
    private static ProblemDetailsFactory GetProblemDetailsFactory(
        HttpContext httpContext)
    {
        return httpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>();
    }

    // Creates an ObjectResult from the Problem Details, setting the status code and content type.
    private static ObjectResult CreateResult(
        ProblemDetails problemDetails)
    {
        var result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };

        result.ContentTypes.Add(ProblemJsonMediaType);

        return result;
    }

    // Adds the application-specific error code and trace ID to the Problem Details extensions.
    private static void AddApplicationMetadata(
        ProblemDetails problemDetails,
        HttpContext httpContext,
        string errorCode)
    {
        problemDetails.Extensions["code"] = errorCode;

        if (!problemDetails.Extensions.ContainsKey("traceId"))
        {
            problemDetails.Extensions["traceId"] =
                Activity.Current?.Id ??
                httpContext.TraceIdentifier;
        }
    }

    // Returns the request path to use as the Problem Details instance.
    private static string? GetInstance(HttpContext httpContext)
    {
        PathString path =
            httpContext.Request.PathBase.Add(httpContext.Request.Path);

        return path.HasValue
            ? path.Value
            : null;
    }
}
