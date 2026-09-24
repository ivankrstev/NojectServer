using Microsoft.Extensions.Options;
using NojectServer.Configurations.Totp;

namespace NojectServer.UnitTests.Configuration;

public sealed class TotpOptionsValidatorTests
{
    private readonly TotpOptionsValidator _validator = new();

    [Theory]
    [InlineData("Example Issuer")]
    [InlineData("Example-Issuer_01")]
    [InlineData(" Example Issuer ")]
    public void Validate_WithValidIssuer_ReturnsSuccess(string issuer)
    {
        ValidateOptionsResult result = _validator.Validate(
            Options.DefaultName,
            new TotpOptions { Issuer = issuer });

        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankIssuer_ReturnsRequiredError(string? issuer)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new TotpOptions { Issuer = issuer! });

        AssertFailures(result, "TOTP issuer is required.");
    }

    [Theory]
    [InlineData("Example:Issuer")]
    [InlineData(":ExampleIssuer")]
    [InlineData("ExampleIssuer:")]
    [InlineData("Example::Issuer")]
    public void Validate_WithIssuerContainingColon_ReturnsFormatError(
        string issuer)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new TotpOptions { Issuer = issuer });

        AssertFailures(result, "TOTP issuer cannot contain a colon.");
    }

    private static void AssertFailures(
        ValidateOptionsResult result,
        string expectedError)
    {
        Assert.True(result.Failed);
        Assert.Equal(expectedError, Assert.Single(result.Failures!));
    }
}
