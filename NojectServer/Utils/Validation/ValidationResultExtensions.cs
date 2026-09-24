using FluentValidation.Results;

namespace NojectServer.Utils.Validation;

/// <summary>
/// Provides extension methods for <see cref="ValidationResult"/> to convert validation errors into a dictionary format.
/// </summary>
/// <remarks>
/// This class is intended to be used in conjunction with the Result pattern to facilitate returning validation errors in a structured format.
/// </remarks>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts the validation errors from a <see cref="ValidationResult"/> into a dictionary where
    /// the keys are property names and the values are arrays of error messages associated with those properties.
    /// </summary>
    /// <param name="validationResult">The validation result containing the errors to convert.</param>
    /// <returns>A dictionary mapping property names to arrays of error messages.</returns>
    public static IReadOnlyDictionary<string, string[]>
        ToErrorDictionary(this ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        return validationResult.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Distinct()
                    .ToArray());
    }
}
