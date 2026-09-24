using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class PendingEmailVerificationTokenOptionsValidatorTests
{
    private readonly PendingEmailVerificationTokenOptionsValidator _validator = new();

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    public void Validate_WithValidOptions_ReturnsSuccess(int expirationInMinutes)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(expirationInMinutes));

        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(31)]
    public void Validate_WithExpirationOutsideAllowedRange_ReturnsRangeError(
        int expirationInMinutes)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(expirationInMinutes));

        AssertFailures(
            result,
            $"{PendingEmailVerificationTokenOptions.SectionName}:ExpirationInMinutes "
            + $"must be between 5 and 30. Configured value: {expirationInMinutes}.");
    }

    [Fact]
    public void Validate_WithMissingSecretKey_ReturnsRequiredError()
    {
        var options = new PendingEmailVerificationTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 15
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{PendingEmailVerificationTokenOptions.SectionName}:SecretKey is required.");
    }

    [Fact]
    public void Validate_WithMalformedSecretKey_ReturnsBase64Error()
    {
        var options = new PendingEmailVerificationTokenOptions
        {
            SecretKey = "not-base64",
            ExpirationInMinutes = 15
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{PendingEmailVerificationTokenOptions.SectionName}:SecretKey "
            + "must be a valid Base64 value.");
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllErrors()
    {
        var options = new PendingEmailVerificationTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 4
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{PendingEmailVerificationTokenOptions.SectionName}:SecretKey is required.",
            $"{PendingEmailVerificationTokenOptions.SectionName}:ExpirationInMinutes "
            + "must be between 5 and 30. Configured value: 4.");
    }

    private static PendingEmailVerificationTokenOptions CreateValidOptions(
        int expirationInMinutes)
    {
        return new PendingEmailVerificationTokenOptions
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
