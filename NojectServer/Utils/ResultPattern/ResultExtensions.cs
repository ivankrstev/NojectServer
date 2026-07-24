using Microsoft.AspNetCore.Mvc;

namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Extension methods for the Result pattern to simplify result handling in controllers.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result object to an appropriate ActionResult by handling success and failure cases.
    /// </summary>
    /// <typeparam name="T">The type of data in the result</typeparam>
    /// <param name="result">The Result object to handle</param>
    /// <param name="controller">The controller instance</param>
    /// <param name="successFunc">A function that processes the successful result value</param>
    /// <returns>An appropriate ActionResult based on the Result</returns>
    public static ActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, ActionResult> successFunc)
    {
        return result switch
        {
            SuccessResult<T> success =>
                successFunc(success.Value),

            FailureResult<T> failure =>
                controller.StatusCode(
                    failure.Error.StatusCode,
                    new
                    {
                        error = failure.Error.Error,
                        message = failure.Error.Message
                    }),

            ValidationFailureResult<T> validationFailure =>
                ToValidationProblem(
                    controller,
                    validationFailure),

            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    /// <summary>
    /// Converts a Result object to an OK ActionResult with the result value.
    /// Simplified version of ToActionResult that returns a 200 OK response with the result value.
    /// </summary>
    /// <typeparam name="T">The type of data in the result</typeparam>
    /// <param name="result">The Result object to convert</param>
    /// <param name="controller">The controller instance</param>
    /// <returns>An appropriate ActionResult based on the Result</returns>
    public static ActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller)
    {
        return result.ToActionResult(controller, value => controller.Ok(value));
    }

    /// <summary>
    /// Converts a Result object to an ActionResult with a specified success status code.
    /// This method allows you to specify a custom status code for successful results.
    /// </summary>
    /// <typeparam name="T">The type of data in the result</typeparam>
    /// <param name="result">The Result object to convert</param>
    /// <param name="controller">The controller instance</param>
    /// <param name="successStatusCode">The status code for successful results</param>
    /// <returns>An appropriate ActionResult based on the Result</returns>
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

    private static ActionResult ToValidationProblem<T>(
        ControllerBase controller,
        ValidationFailureResult<T> validationFailure)
    {
        var errors =
            validationFailure.ValidationErrors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value);

        ValidationProblemDetails problemDetails =
            new(errors)
            {
                Status = validationFailure.Error.StatusCode,
                Title = validationFailure.Error.Message
            };

        problemDetails.Extensions["error"] =
            validationFailure.Error.Error;

        return controller.ValidationProblem(problemDetails);
    }
}
