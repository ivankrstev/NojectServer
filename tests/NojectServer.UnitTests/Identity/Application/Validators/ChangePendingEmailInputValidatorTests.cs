using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class ChangePendingEmailInputValidatorTests
{
    private readonly ChangePendingEmailInputValidator _validator = new();

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
            new ChangePendingEmailInput(email));

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
            CreateValidInput() with { NewEmail = email },
            nameof(ChangePendingEmailInput.NewEmail),
            "New email is required.");
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
                NewEmail = emailLocalPart + "@example.com"
            },
            nameof(ChangePendingEmailInput.NewEmail),
            $"New email cannot exceed {User.MaximumEmailLength} characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmailAddress_ReturnsFormatError()
    {
        AssertSingleError(
            CreateValidInput() with { NewEmail = "not-an-email" },
            nameof(ChangePendingEmailInput.NewEmail),
            "New email must be a valid email address.");
    }

    private static ChangePendingEmailInput CreateValidInput()
    {
        return new ChangePendingEmailInput("person@example.com");
    }

    private void AssertSingleError(
        ChangePendingEmailInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
