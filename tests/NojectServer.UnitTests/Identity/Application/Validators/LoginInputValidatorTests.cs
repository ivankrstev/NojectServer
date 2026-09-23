using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class LoginInputValidatorTests
{
    private readonly LoginInputValidator _validator = new();

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
        string password = new('P', PasswordPolicy.MaximumLength);

        ValidationResult result = _validator.Validate(
            new LoginInput(email, password));

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
            nameof(LoginInput.Email),
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
            nameof(LoginInput.Email),
            $"Email cannot exceed {User.MaximumEmailLength} characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmailAddress_ReturnsFormatError()
    {
        AssertSingleError(
            CreateValidInput() with { Email = "not-an-email" },
            nameof(LoginInput.Email),
            "Email must be a valid email address.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingPassword_ReturnsRequiredError(string? password)
    {
        AssertSingleError(
            CreateValidInput() with { Password = password },
            nameof(LoginInput.Password),
            "Password is required.");
    }

    [Fact]
    public void Validate_WithPasswordLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string password = new('P', PasswordPolicy.MaximumLength + 1);

        AssertSingleError(
            CreateValidInput() with { Password = password },
            nameof(LoginInput.Password),
            $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }

    private static LoginInput CreateValidInput()
    {
        return new LoginInput(
            "person@example.com",
            "password");
    }

    private void AssertSingleError(
        LoginInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
