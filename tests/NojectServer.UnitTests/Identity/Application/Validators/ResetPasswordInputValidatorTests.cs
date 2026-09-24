using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Application.Passwords;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class ResetPasswordInputValidatorTests
{
    //TODO: Consider moving the reset-token maximum length, into a shared policy constant, if it is used in multiple places.
    private const int MaximumTokenLength = 128;

    private readonly ResetPasswordInputValidator _validator = new();

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
        string resetToken = new('a', MaximumTokenLength);
        string password = new('P', PasswordPolicy.MaximumLength);

        ValidationResult result = _validator.Validate(
            new ResetPasswordInput(resetToken, password, password));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingResetToken_ReturnsRequiredError(
        string? resetToken)
    {
        AssertSingleError(
            CreateValidInput() with { ResetToken = resetToken },
            nameof(ResetPasswordInput.ResetToken),
            "Reset token is required.");
    }

    [Fact]
    public void Validate_WithResetTokenLongerThanMaximum_ReturnsMaximumLengthError()
    {
        AssertSingleError(
            CreateValidInput() with
            {
                ResetToken = new string('a', MaximumTokenLength + 1)
            },
            nameof(ResetPasswordInput.ResetToken),
            $"Reset token cannot exceed {MaximumTokenLength} characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingNewPassword_ReturnsRequiredError(
        string? newPassword)
    {
        AssertHasError(
            CreateValidInput() with { NewPassword = newPassword },
            nameof(ResetPasswordInput.NewPassword),
            "New password is required.");
    }

    [Fact]
    public void Validate_WithNewPasswordAtMinimumLength_IsValid()
    {
        string password = new('P', PasswordPolicy.MinimumLength);

        ValidationResult result = _validator.Validate(
            CreateValidInput() with
            {
                NewPassword = password,
                ConfirmNewPassword = password
            });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNewPasswordShorterThanMinimum_ReturnsMinimumLengthError()
    {
        string password = new('P', PasswordPolicy.MinimumLength - 1);

        AssertSingleError(
            CreateValidInput() with
            {
                NewPassword = password,
                ConfirmNewPassword = password
            },
            nameof(ResetPasswordInput.NewPassword),
            $"New password must contain at least {PasswordPolicy.MinimumLength} characters.");
    }

    [Fact]
    public void Validate_WithNewPasswordLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string password = new('P', PasswordPolicy.MaximumLength + 1);

        AssertSingleError(
            CreateValidInput() with
            {
                NewPassword = password,
                ConfirmNewPassword = password
            },
            nameof(ResetPasswordInput.NewPassword),
            $"New password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingPasswordConfirmation_ReturnsRequiredError(
        string? confirmNewPassword)
    {
        AssertSingleError(
            CreateValidInput() with
            {
                ConfirmNewPassword = confirmNewPassword
            },
            nameof(ResetPasswordInput.ConfirmNewPassword),
            "New password confirmation is required.");
    }

    [Fact]
    public void Validate_WithDifferentPasswordConfirmation_ReturnsMismatchError()
    {
        string differentPassword = new('D', PasswordPolicy.MinimumLength);

        AssertSingleError(
            CreateValidInput() with
            {
                ConfirmNewPassword = differentPassword
            },
            nameof(ResetPasswordInput.ConfirmNewPassword),
            "Passwords do not match.");
    }

    private static ResetPasswordInput CreateValidInput()
    {
        string password = new('P', PasswordPolicy.MinimumLength);

        return new ResetPasswordInput(
            "reset-token",
            password,
            password);
    }

    private void AssertSingleError(
        ResetPasswordInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    private void AssertHasError(
        ResetPasswordInput input,
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
