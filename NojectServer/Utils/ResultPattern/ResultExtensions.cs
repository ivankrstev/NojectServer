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
            SuccessResult<T> success => successFunc(success.Value),
            FailureResult<T> failure => controller.StatusCode(
                failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
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
}
