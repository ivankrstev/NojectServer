using Microsoft.AspNetCore.Mvc;

namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Converts application results to ASP.NET Core action results.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a non-generic result to an action result,
    /// using the provided function to map a success result.
    /// </summary>
    public static ActionResult ToActionResult(
        this Result result,
        ControllerBase controller,
        Func<ActionResult> successFunc)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(successFunc);

        return result switch
        {
            SuccessResult =>
                successFunc(),

            ValidationFailureResult validationFailure =>
                ToValidationProblem(
                    controller,
                    validationFailure.Error,
                    validationFailure.ValidationErrors),

            FailureResult failure =>
                ToFailureActionResult(controller, failure.Error),

            _ => throw new InvalidOperationException(
                $"Unsupported result type: {result.GetType().Name}.")
        };
    }

    /// <summary>
    /// Maps a payload-free success to 204 No Content.
    /// </summary>
    public static ActionResult ToActionResult(
        this Result result,
        ControllerBase controller)
    {
        return result.ToActionResult(
            controller,
            controller.NoContent);
    }

    /// <summary>
    /// Maps a payload-free success to the specified status code.
    /// </summary>
    public static ActionResult ToActionResult(
        this Result result,
        ControllerBase controller,
        int successStatusCode)
    {
        return result.ToActionResult(
            controller,
            () => controller.StatusCode(successStatusCode));
    }

    /// <summary>
    /// Converts a generic result to an action result,
    /// using the provided function to map the successful value.
    /// </summary>
    public static ActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, ActionResult> successFunc)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(successFunc);

        return result switch
        {
            SuccessResult<T> success =>
                successFunc(success.Value),

            ValidationFailureResult<T> validationFailure =>
                ToValidationProblem(
                    controller,
                    validationFailure.Error,
                    validationFailure.ValidationErrors),

            FailureResult<T> failure =>
                ToFailureActionResult(controller, failure.Error),

            _ => throw new InvalidOperationException(
                "Unknown generic result type.")
        };
    }

    /// <summary>
    /// Maps a value-returning success to 200 OK.
    /// </summary>
    public static ActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller)
    {
        return result.ToActionResult(
            controller,
            value => controller.Ok(value));
    }

    /// <summary>
    /// Maps a value-returning success to the specified status code.
    /// </summary>
    public static ActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        int successStatusCode)
    {
        return result.ToActionResult(
            controller,
            value => controller.StatusCode(
                successStatusCode,
                value));
    }

    /// <summary>
    /// Converts general error details to an HTTP error response.
    /// </summary>
    private static ActionResult ToFailureActionResult(
        ControllerBase controller,
        ErrorDetails error)
    {
        return ApiErrorResponseFactory.CreateFailure(
            controller.HttpContext,
            error);
    }

    /// <summary>
    /// Converts field-specific validation errors to an HTTP validation
    /// problem response.
    /// </summary>
    private static ActionResult ToValidationProblem(
        ControllerBase controller,
        ErrorDetails error,
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        return ApiErrorResponseFactory.CreateValidation(
            controller.HttpContext,
            error,
            validationErrors);
    }
}
