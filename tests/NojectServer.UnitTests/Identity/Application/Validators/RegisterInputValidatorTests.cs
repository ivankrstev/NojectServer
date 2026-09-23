using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.Register;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class RegisterInputValidatorTests
{
    private readonly RegisterInputValidator _validator = new();

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
        string fullName = new('N', User.MaximumFullNameLength);
        string password = new('P', PasswordPolicy.MaximumLength);

        ValidationResult result = _validator.Validate(
            new RegisterInput(email, fullName, password, password));

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
            nameof(RegisterInput.Email),
            "Email is required.");
    }

    [Fact]
    public void Validate_WithEmailLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string email = new(
            'a',
            User.MaximumEmailLength - "@example.com".Length + 1);

        AssertSingleError(
            CreateValidInput() with { Email = email + "@example.com" },
            nameof(RegisterInput.Email),
            $"Email cannot exceed {User.MaximumEmailLength} characters.");
    }

    [Fact]
    public void Validate_WithInvalidEmailAddress_ReturnsFormatError()
    {
        AssertSingleError(
            CreateValidInput() with { Email = "not-an-email" },
            nameof(RegisterInput.Email),
            "Email must be a valid email address.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingFullName_ReturnsRequiredError(string? fullName)
    {
        AssertSingleError(
            CreateValidInput() with { FullName = fullName },
            nameof(RegisterInput.FullName),
            "Full name is required.");
    }

    [Fact]
    public void Validate_WithFullNameLongerThanMaximum_ReturnsMaximumLengthError()
    {
        AssertSingleError(
            CreateValidInput() with
            {
                FullName = new string('N', User.MaximumFullNameLength + 1)
            },
            nameof(RegisterInput.FullName),
            $"Full name cannot exceed {User.MaximumFullNameLength} characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingPassword_ReturnsRequiredError(string? password)
    {
        AssertHasError(
            CreateValidInput() with { Password = password },
            nameof(RegisterInput.Password),
            "Password is required.");
    }

    [Fact]
    public void Validate_WithPasswordAtMinimumLength_IsValid()
    {
        string password = new('P', PasswordPolicy.MinimumLength);

        ValidationResult result = _validator.Validate(
            CreateValidInput() with
            {
                Password = password,
                ConfirmPassword = password
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithPasswordShorterThanMinimum_ReturnsMinimumLengthError()
    {
        string password = new('P', PasswordPolicy.MinimumLength - 1);

        AssertSingleError(
            CreateValidInput() with
            {
                Password = password,
                ConfirmPassword = password
            },
            nameof(RegisterInput.Password),
            $"Password must contain at least {PasswordPolicy.MinimumLength} characters.");
    }

    [Fact]
    public void Validate_WithPasswordLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string password = new('P', PasswordPolicy.MaximumLength + 1);

        AssertSingleError(
            CreateValidInput() with
            {
                Password = password,
                ConfirmPassword = password
            },
            nameof(RegisterInput.Password),
            $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingPasswordConfirmation_ReturnsRequiredError(
        string? confirmPassword)
    {
        AssertSingleError(
            CreateValidInput() with { ConfirmPassword = confirmPassword },
            nameof(RegisterInput.ConfirmPassword),
            "Password confirmation is required.");
    }

    [Fact]
    public void Validate_WithDifferentPasswordConfirmation_ReturnsMismatchError()
    {
        string differentPassword = new('D', PasswordPolicy.MinimumLength);

        AssertSingleError(
            CreateValidInput() with { ConfirmPassword = differentPassword },
            nameof(RegisterInput.ConfirmPassword),
            "Passwords do not match.");
    }

    private static RegisterInput CreateValidInput()
    {
        string password = new('P', PasswordPolicy.MinimumLength);

        return new RegisterInput(
            "person@example.com",
            "Person Example",
            password,
            password);
    }

    private void AssertSingleError(
        RegisterInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    private void AssertHasError(
        RegisterInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == propertyName
                && failure.ErrorMessage == errorMessage);
    }
}
