using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class JwtSigningKeyValidatorTests
{
    private const string ConfigurationPath = "Jwt:Access:SecretKey";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithMissingKey_ReturnsRequiredError(string? secretKey)
    {
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(secretKey, ConfigurationPath, errors);

        Assert.Equal(
            [$"{ConfigurationPath} is required."],
            errors);
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("AA=A")]
    [InlineData("*")]
    public void Validate_WithMalformedBase64_ReturnsFormatError(string secretKey)
    {
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(secretKey, ConfigurationPath, errors);

        Assert.Equal(
            [$"{ConfigurationPath} must be a valid Base64 value."],
            errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Validate_WithKeyShorterThanMinimum_ReturnsSizeError(
        int keySizeInBytes)
    {
        string secretKey = Convert.ToBase64String(new byte[keySizeInBytes]);
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(secretKey, ConfigurationPath, errors);

        Assert.Equal(
            [
                $"{ConfigurationPath} must decode to at least 32 bytes for "
                + $"HS256. Current size: {keySizeInBytes} bytes."
            ],
            errors);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void Validate_WithKeyMeetingMinimum_ReturnsNoErrors(int keySizeInBytes)
    {
        string secretKey = Convert.ToBase64String(new byte[keySizeInBytes]);
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(secretKey, ConfigurationPath, errors);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithExistingErrors_AppendsOnlyNewValidationErrors()
    {
        List<string> errors = ["previous error"];

        JwtSigningKeyValidator.Validate(
            Convert.ToBase64String(new byte[32]),
            ConfigurationPath,
            errors);

        Assert.Equal(["previous error"], errors);
    }

    [Fact]
    public void Validate_UsesProvidedConfigurationPathInError()
    {
        const string configurationPath = "Jwt:Tfa:SecretKey";
        List<string> errors = [];

        JwtSigningKeyValidator.Validate("invalid", configurationPath, errors);

        Assert.Equal(
            [$"{configurationPath} must be a valid Base64 value."],
            errors);
    }
}
