using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class TfaTokenOptionsValidatorTests
{
    private readonly TfaTokenOptionsValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Validate_WithValidOptions_ReturnsSuccess(int expirationInMinutes)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(expirationInMinutes));

        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validate_WithExpirationOutsideAllowedRange_ReturnsRangeError(
        int expirationInMinutes)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(expirationInMinutes));

        AssertFailures(
            result,
            $"{TfaTokenOptions.SectionName}:ExpirationInMinutes must be between "
            + $"1 and 10. Configured value: {expirationInMinutes}.");
    }

    [Fact]
    public void Validate_WithMissingSecretKey_ReturnsRequiredError()
    {
        var options = new TfaTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 5
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{TfaTokenOptions.SectionName}:SecretKey is required.");
    }

    [Fact]
    public void Validate_WithMalformedSecretKey_ReturnsBase64Error()
    {
        var options = new TfaTokenOptions
        {
            SecretKey = "not-base64",
            ExpirationInMinutes = 5
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{TfaTokenOptions.SectionName}:SecretKey must be a valid Base64 value.");
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllErrors()
    {
        var options = new TfaTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 0
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{TfaTokenOptions.SectionName}:SecretKey is required.",
            $"{TfaTokenOptions.SectionName}:ExpirationInMinutes must be between "
            + "1 and 10. Configured value: 0.");
    }

    private static TfaTokenOptions CreateValidOptions(int expirationInMinutes)
    {
        return new TfaTokenOptions
        {
            SecretKey = CreateSigningKey(),
            ExpirationInMinutes = expirationInMinutes
        };
    }

    private static string CreateSigningKey()
    {
        return Convert.ToBase64String(new byte[32]);
    }

    private static void AssertFailures(
        ValidateOptionsResult result,
        params string[] expectedErrors)
    {
        Assert.True(result.Failed);
        Assert.Equal(expectedErrors, result.Failures!);
    }
}
