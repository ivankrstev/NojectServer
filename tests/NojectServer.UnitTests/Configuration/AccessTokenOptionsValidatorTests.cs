using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class AccessTokenOptionsValidatorTests
{
    private readonly AccessTokenOptionsValidator _validator = new();

    [Theory]
    [InlineData(1)]
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
    [InlineData(0)]
    [InlineData(31)]
    public void Validate_WithExpirationOutsideAllowedRange_ReturnsRangeError(
        int expirationInMinutes)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(expirationInMinutes));

        AssertFailures(
            result,
            $"{AccessTokenOptions.SectionName}:ExpirationInMinutes must be between "
            + $"1 and 30. Configured value: {expirationInMinutes}.");
    }

    [Fact]
    public void Validate_WithMissingSecretKey_ReturnsRequiredError()
    {
        var options = new AccessTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 10
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{AccessTokenOptions.SectionName}:SecretKey is required.");
    }

    [Fact]
    public void Validate_WithMalformedSecretKey_ReturnsBase64Error()
    {
        var options = new AccessTokenOptions
        {
            SecretKey = "not-base64",
            ExpirationInMinutes = 10
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{AccessTokenOptions.SectionName}:SecretKey must be a valid Base64 value.");
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllErrors()
    {
        var options = new AccessTokenOptions
        {
            SecretKey = " ",
            ExpirationInMinutes = 0
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{AccessTokenOptions.SectionName}:SecretKey is required.",
            $"{AccessTokenOptions.SectionName}:ExpirationInMinutes must be between "
            + "1 and 30. Configured value: 0.");
    }

    private static AccessTokenOptions CreateValidOptions(int expirationInMinutes)
    {
        return new AccessTokenOptions
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
