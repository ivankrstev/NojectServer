namespace NojectServer.UnitTests.Identity.Infrastructure.TwoFactorAuthentication;

public sealed class TotpWindowTests
{
    [Fact]
    public void TryValidateCode_UsesInjectedTimeProviderWhenTimeChanges()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        var service = TotpTestSupport.CreateService(timeProvider);
        byte[] secret = TotpTestSupport.CreateKnownSecret();

        bool firstResult = service.TryValidateCode(
            secret,
            TotpTestSupport.ComputeSixDigitCode(secret, 100),
            out long firstMatchedTimeStep);

        timeProvider.SetUtcNow(TotpTestSupport.TimeAtStep(101));

        bool secondResult = service.TryValidateCode(
            secret,
            TotpTestSupport.ComputeSixDigitCode(secret, 101),
            out long secondMatchedTimeStep);

        Assert.True(firstResult);
        Assert.Equal(100L, firstMatchedTimeStep);
        Assert.True(secondResult);
        Assert.Equal(101L, secondMatchedTimeStep);
    }

    [Theory]
    [InlineData(99L)]
    [InlineData(100L)]
    [InlineData(101L)]
    public void TryValidateCode_AcceptsCurrentAndOneAdjacentTimeStep(
        long codeTimeStep)
    {
        const long currentTimeStep = 100;
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(currentTimeStep));
        var service = TotpTestSupport.CreateService(timeProvider);
        byte[] secret = TotpTestSupport.CreateKnownSecret();

        bool result = service.TryValidateCode(
            secret,
            TotpTestSupport.ComputeSixDigitCode(secret, codeTimeStep),
            out long matchedTimeStep);

        Assert.True(result);
        Assert.Equal(codeTimeStep, matchedTimeStep);
    }

    [Theory]
    [InlineData(98L)]
    [InlineData(102L)]
    public void TryValidateCode_RejectsCodesBeyondOneAdjacentTimeStep(
        long codeTimeStep)
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100));
        var service = TotpTestSupport.CreateService(timeProvider);
        byte[] secret = TotpTestSupport.CreateKnownSecret();

        bool result = service.TryValidateCode(
            secret,
            TotpTestSupport.ComputeSixDigitCode(secret, codeTimeStep),
            out long matchedTimeStep);

        Assert.False(result);
        Assert.Equal(0L, matchedTimeStep);
    }

    [Fact]
    public void TryValidateCode_HandlesExactThirtySecondBoundary()
    {
        var timeProvider = new AdjustableTimeProvider(
            TotpTestSupport.TimeAtStep(100, secondsIntoStep: 29));
        var service = TotpTestSupport.CreateService(timeProvider);
        byte[] secret = TotpTestSupport.CreateKnownSecret();
        string codeThatIsTwoStepsAhead = TotpTestSupport.ComputeSixDigitCode(
            secret,
            102);

        bool beforeBoundaryResult = service.TryValidateCode(
            secret,
            codeThatIsTwoStepsAhead,
            out long beforeBoundaryMatchedTimeStep);

        timeProvider.SetUtcNow(TotpTestSupport.TimeAtStep(101));

        bool atBoundaryResult = service.TryValidateCode(
            secret,
            codeThatIsTwoStepsAhead,
            out long atBoundaryMatchedTimeStep);

        Assert.False(beforeBoundaryResult);
        Assert.Equal(0L, beforeBoundaryMatchedTimeStep);
        Assert.True(atBoundaryResult);
        Assert.Equal(102L, atBoundaryMatchedTimeStep);
    }
}
