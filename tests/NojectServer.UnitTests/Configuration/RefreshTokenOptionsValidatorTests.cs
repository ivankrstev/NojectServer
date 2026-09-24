using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;

namespace NojectServer.UnitTests.Configuration;

public sealed class RefreshTokenOptionsValidatorTests
{
    private readonly RefreshTokenOptionsValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    public void Validate_WithExpirationAtAllowedBoundary_ReturnsSuccess(
        int expirationInDays)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new RefreshTokenOptions
            {
                ExpirationInDays = expirationInDays
            });

        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(91)]
    public void Validate_WithExpirationOutsideAllowedRange_ReturnsRangeError(
        int expirationInDays)
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new RefreshTokenOptions
            {
                ExpirationInDays = expirationInDays
            });

        Assert.True(result.Failed);
        Assert.Equal(
            $"{RefreshTokenOptions.SectionName}:ExpirationInDays must be between "
            + $"1 and 90. Configured value: {expirationInDays}.",
            Assert.Single(result.Failures!));
    }
}
