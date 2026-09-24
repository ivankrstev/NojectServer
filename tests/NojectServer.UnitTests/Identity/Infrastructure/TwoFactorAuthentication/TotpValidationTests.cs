using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.UnitTests.Identity.Infrastructure.TwoFactorAuthentication;

public sealed class TotpValidationTests
{
    [Theory]
    [InlineData(59L, "287082", 1L)]
    [InlineData(1_111_111_109L, "081804", 37_037_036L)]
    [InlineData(1_111_111_111L, "050471", 37_037_037L)]
    [InlineData(1_234_567_890L, "005924", 41_152_263L)]
    [InlineData(2_000_000_000L, "279037", 66_666_666L)]
    [InlineData(20_000_000_000L, "353130", 666_666_666L)]
    public void TryValidateCode_WithIndependentlyKnownSixDigitCodes_ReturnsTrue(
        long unixTimeSeconds,
        string code,
        long expectedTimeStep)
    {
        var timeProvider = new AdjustableTimeProvider(
            DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);

        bool result = service.TryValidateCode(
            TotpTestSupport.CreateKnownSecret(),
            code,
            out long matchedTimeStep);

        Assert.True(result);
        Assert.Equal(expectedTimeStep, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_AcceptsLeadingZeroCode()
    {
        var timeProvider = new AdjustableTimeProvider(
            DateTimeOffset.FromUnixTimeSeconds(1_234_567_890));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);

        bool result = service.TryValidateCode(
            TotpTestSupport.CreateKnownSecret(),
            "005924",
            out long matchedTimeStep);

        Assert.True(result);
        Assert.Equal(41_152_263L, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_TrimsSurroundingWhitespace()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);
        string code = TotpTestSupport.ComputeSixDigitCode(
            TotpTestSupport.CreateKnownSecret(),
            100);

        bool result = service.TryValidateCode(
            TotpTestSupport.CreateKnownSecret(),
            $"  {code}\t",
            out long matchedTimeStep);

        Assert.True(result);
        Assert.Equal(100L, matchedTimeStep);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData("123-56")]
    [InlineData("１２３４５６")]
    public void TryValidateCode_WithMalformedCode_ReturnsFalseAndDefaultTimeStep(
        string? code)
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);

        bool result = service.TryValidateCode(
            TotpTestSupport.CreateKnownSecret(),
            code,
            out long matchedTimeStep);

        Assert.False(result);
        Assert.Equal(0L, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_WithWrongCode_ReturnsFalse()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);
        string validCode = TotpTestSupport.ComputeSixDigitCode(
            TotpTestSupport.CreateKnownSecret(),
            100);
        string wrongCode = validCode == "000000" ? "000001" : "000000";

        bool result = service.TryValidateCode(
            TotpTestSupport.CreateKnownSecret(),
            wrongCode,
            out long matchedTimeStep);

        Assert.False(result);
        Assert.Equal(0L, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_WithWrongSecret_ReturnsFalse()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);
        string code = TotpTestSupport.ComputeSixDigitCode(
            TotpTestSupport.CreateKnownSecret(),
            100);
        byte[] wrongSecret = TotpTestSupport.CreateKnownSecret();
        wrongSecret[^1]++;

        bool result = service.TryValidateCode(
            wrongSecret,
            code,
            out long matchedTimeStep);

        Assert.False(result);
        Assert.Equal(0L, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_WithNullSecret_ThrowsArgumentNullException()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);

        Assert.Throws<ArgumentNullException>(() => service.TryValidateCode(
            null!,
            "000000",
            out _));
    }

    [Fact]
    public void TryValidateCode_WithEmptySecret_ThrowsArgumentException()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        OtpNetTotpService service = TotpTestSupport.CreateService(timeProvider);

        Assert.Throws<ArgumentException>(() => service.TryValidateCode(
            [],
            "000000",
            out _));
    }
}
