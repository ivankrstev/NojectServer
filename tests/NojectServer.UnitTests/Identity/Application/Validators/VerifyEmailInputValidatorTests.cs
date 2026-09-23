using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.EmailVerification;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class VerifyEmailInputValidatorTests
{
    private const int MaximumTokenLength = 128;

    private readonly VerifyEmailInputValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateValidInput());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithValuesAtMaximumLengths_IsValid()
    {
        string emailLocalPart = new(
            'a',
            User.MaximumEmailLength - "@example.com".Length);
        string email = emailLocalPart + "@example.com";
        string token = new('a', MaximumTokenLength);

        ValidationResult result = _validator.Validate(
            new VerifyEmailInput(email, token));

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
            nameof(VerifyEmailInput.Email),
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
            nameof(VerifyEmailInput.Email),
            $"Email cannot exceed {User.MaximumEmailLength} characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmailAddress_ReturnsFormatError()
    {
        AssertSingleError(
            CreateValidInput() with { Email = "not-an-email" },
            nameof(VerifyEmailInput.Email),
            "Email must be a valid email address.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingToken_ReturnsRequiredError(string? token)
    {
        AssertSingleError(
            CreateValidInput() with { Token = token },
            nameof(VerifyEmailInput.Token),
            "Token is required.");
    }

    [Fact]
    public void Validate_WithTokenLongerThanMaximum_ReturnsMaximumLengthError()
    {
        AssertSingleError(
            CreateValidInput() with
            {
                Token = new string('a', MaximumTokenLength + 1)
            },
            nameof(VerifyEmailInput.Token),
            $"Token cannot exceed {MaximumTokenLength} characters.");
    }

    private static VerifyEmailInput CreateValidInput()
    {
        return new VerifyEmailInput("person@example.com", "verification-token");
    }

    private void AssertSingleError(
        VerifyEmailInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
