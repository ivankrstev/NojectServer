using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.UnitTests.Identity.Application.Validators;

public sealed class CompleteTwoFactorLoginInputValidatorTests
{
    private readonly CompleteTwoFactorLoginInputValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateValidInput());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithMaximumLengthToken_IsValid()
    {
        string token = new string('a', 508) + ".b.c";

        ValidationResult result = _validator.Validate(
            new CompleteTwoFactorLoginInput(
                token,
                new string('1', OtpNetTotpService.CodeSizeInDigits)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingTfaToken_ReturnsRequiredError(string? token)
    {
        AssertSingleError(
            CreateValidInput() with { TfaToken = token },
            nameof(CompleteTwoFactorLoginInput.TfaToken),
            "Tfa token is required.");
    }

    [Fact]
    public void Validate_WithTfaTokenLongerThanMaximum_ReturnsMaximumLengthError()
    {
        string token = new('a', 513);

        AssertSingleError(
            CreateValidInput() with { TfaToken = token },
            nameof(CompleteTwoFactorLoginInput.TfaToken),
            "Tfa token cannot exceed 512 characters.");
    }

    [Theory]
    [InlineData("token")]
    [InlineData("header.payload")]
    [InlineData(".payload.signature")]
    [InlineData("header..signature")]
    [InlineData("header.payload.signature!")]
    [InlineData(" header.payload.signature")]
    public void Validate_WithMalformedTfaToken_ReturnsFormatError(string token)
    {
        AssertSingleError(
            CreateValidInput() with { TfaToken = token },
            nameof(CompleteTwoFactorLoginInput.TfaToken),
            "Tfa token is not a well-formed token.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithMissingCode_ReturnsRequiredError(string? code)
    {
        AssertSingleError(
            CreateValidInput() with { Code = code },
            nameof(CompleteTwoFactorLoginInput.Code),
            "Code is required.");
    }

    [Fact]
    public void Validate_WithCodeShorterThanRequired_ReturnsLengthError()
    {
        string code = new(
            '1',
            OtpNetTotpService.CodeSizeInDigits - 1);

        AssertSingleError(
            CreateValidInput() with { Code = code },
            nameof(CompleteTwoFactorLoginInput.Code),
            $"Code must be exactly {OtpNetTotpService.CodeSizeInDigits} characters.");
    }

    [Fact]
    public void Validate_WithCodeLongerThanRequired_ReturnsLengthError()
    {
        string code = new(
            '1',
            OtpNetTotpService.CodeSizeInDigits + 1);

        AssertSingleError(
            CreateValidInput() with { Code = code },
            nameof(CompleteTwoFactorLoginInput.Code),
            $"Code must be exactly {OtpNetTotpService.CodeSizeInDigits} characters.");
    }

    [Fact]
    public void Validate_WithNonNumericCode_ReturnsDigitsOnlyError()
    {
        string code = "12345a";

        AssertSingleError(
            CreateValidInput() with { Code = code },
            nameof(CompleteTwoFactorLoginInput.Code),
            "Code must contain only digits.");
    }

    private static CompleteTwoFactorLoginInput CreateValidInput()
    {
        return new CompleteTwoFactorLoginInput(
            "header.payload.signature",
            new string('1', OtpNetTotpService.CodeSizeInDigits));
    }

    private void AssertSingleError(
        CompleteTwoFactorLoginInput input,
        string propertyName,
        string errorMessage)
    {
        ValidationResult result = _validator.Validate(input);
        ValidationFailure failure = Assert.Single(result.Errors);

        Assert.Equal(propertyName, failure.PropertyName);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }
}
