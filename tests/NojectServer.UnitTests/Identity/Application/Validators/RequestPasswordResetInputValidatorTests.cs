using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class RequestPasswordResetInputValidatorTests
{
    private readonly RequestPasswordResetInputValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateValidInput());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmailAtMaximumLength_IsValid()
    {
        string emailLocalPart = new(
            'a',
            User.MaximumEmailLength - "@example.com".Length);
        string email = emailLocalPart + "@example.com";

        ValidationResult result = _validator.Validate(
            new RequestPasswordResetInput(email));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingEmail_ReturnsRequiredError(string? email)
    {
        AssertSingleError(
            CreateValidInput() with { Email = email },
            nameof(RequestPasswordResetInput.Email),
            "Email is required.");
    }

    [Fact]
    public void Validate_WithEmailLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string emailLocalPart = new(
            'a',
            User.MaximumEmailLength - "@example.com".Length + 1);

        AssertSingleError(
            CreateValidInput() with
            {
                Email = emailLocalPart + "@example.com"
            },
            nameof(RequestPasswordResetInput.Email),
            $"Email cannot exceed {User.MaximumEmailLength} characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmailAddress_ReturnsFormatError()
    {
        AssertSingleError(
            CreateValidInput() with { Email = "not-an-email" },
            nameof(RequestPasswordResetInput.Email),
            "Email must be a valid email address.");
    }

    private static RequestPasswordResetInput CreateValidInput()
    {
        return new RequestPasswordResetInput("person@example.com");
    }

    private void AssertSingleError(
        RequestPasswordResetInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
