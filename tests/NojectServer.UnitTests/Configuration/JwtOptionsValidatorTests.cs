using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        ValidateOptionsResult result = _validator.Validate(
            Options.DefaultName,
            CreateValidOptions());

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
        JwtOptions options = CreateValidOptions();
        options = new JwtOptions
        {
            Issuer = issuer!,
            Audience = options.Audience,
            ClockSkewInSeconds = options.ClockSkewInSeconds
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(result, $"{JwtOptions.SectionName}:Issuer is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankAudience_ReturnsRequiredError(string? audience)
    {
        JwtOptions options = CreateValidOptions();
        options = new JwtOptions
        {
            Issuer = options.Issuer,
            Audience = audience!,
            ClockSkewInSeconds = options.ClockSkewInSeconds
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(result, $"{JwtOptions.SectionName}:Audience is required");
    }

    [Fact]
    public void Validate_WithMissingClockSkew_ReturnsRequiredError()
    {
        var options = new JwtOptions
        {
            Issuer = "NojectServer.Tests",
            Audience = "NojectClient.Tests",
            ClockSkewInSeconds = null
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{JwtOptions.SectionName}:ClockSkewInSeconds is required");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Validate_WithClockSkewOutsideAllowedRange_ReturnsRangeError(
        int clockSkewInSeconds)
    {
        JwtOptions options = CreateValidOptions(clockSkewInSeconds);

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{JwtOptions.SectionName}:ClockSkewInSeconds must be between 0 and 60.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void Validate_WithClockSkewAtBoundary_ReturnsSuccess(
        int clockSkewInSeconds)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            CreateValidOptions(clockSkewInSeconds));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllErrors()
    {
        var options = new JwtOptions
        {
            Issuer = " ",
            Audience = "",
            ClockSkewInSeconds = -1
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            [
                $"{JwtOptions.SectionName}:Issuer is required",
                $"{JwtOptions.SectionName}:Audience is required",
                $"{JwtOptions.SectionName}:ClockSkewInSeconds must be between 0 and 60."
            ],
            result.Failures);
    }

    private static JwtOptions CreateValidOptions(int clockSkewInSeconds = 30)
    {
        return new JwtOptions
        {
            Issuer = "NojectServer.Tests",
            Audience = "NojectClient.Tests",
            ClockSkewInSeconds = clockSkewInSeconds
        };
    }

    private static void AssertFailures(
        ValidateOptionsResult result,
        string expectedError)
    {
        Assert.True(result.Failed);
        Assert.Equal(expectedError, Assert.Single(result.Failures!));
    }
}
