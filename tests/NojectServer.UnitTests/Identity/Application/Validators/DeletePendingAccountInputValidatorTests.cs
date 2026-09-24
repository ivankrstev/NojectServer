using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;
using NojectServer.Modules.Identity.Application.Passwords;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class DeletePendingAccountInputValidatorTests
{
    private readonly DeletePendingAccountInputValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateValidInput());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithPasswordAtMaximumLength_IsValid()
    {
        string password = new('P', PasswordPolicy.MaximumLength);

        ValidationResult result = _validator.Validate(
            new DeletePendingAccountInput(password));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingPassword_ReturnsRequiredError(
        string? password)
    {
        AssertSingleError(
            CreateValidInput() with { Password = password },
            nameof(DeletePendingAccountInput.Password),
            "Password is required.");
    }

    [Fact]
    public void Validate_WithPasswordLongerThanMaximum_ReturnsMaximumLengthError()
    {
        AssertSingleError(
            CreateValidInput() with
            {
                Password = new string('P', PasswordPolicy.MaximumLength + 1)
            },
            nameof(DeletePendingAccountInput.Password),
            $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }

    private static DeletePendingAccountInput CreateValidInput()
    {
        return new DeletePendingAccountInput("pending-account-password");
    }

    private void AssertSingleError(
        DeletePendingAccountInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
