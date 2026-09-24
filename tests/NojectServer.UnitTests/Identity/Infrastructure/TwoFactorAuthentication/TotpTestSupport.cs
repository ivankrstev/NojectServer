using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NojectServer.Configurations.Totp;
using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.UnitTests.Identity.Infrastructure.TwoFactorAuthentication;

internal static class TotpTestSupport
{
    public static byte[] CreateKnownSecret()
    {
        return Encoding.ASCII.GetBytes("12345678901234567890");
    }

    public static OtpNetTotpService CreateService(
        TimeProvider timeProvider,
        string issuer = "Example Issuer")
    {
        return new OtpNetTotpService(
            Options.Create(new TotpOptions { Issuer = issuer }),
            timeProvider);
    }

    // Independent six-digit SHA-1 TOTP calculation used to create test inputs.
    public static string ComputeSixDigitCode(
        byte[] secret,
        long timeStep)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);

        using var hmac = new HMACSHA1(secret);
        byte[] digest = hmac.ComputeHash(counter.ToArray());
        int offset = digest[^1] & 0x0F;
        int binaryCode =
            ((digest[offset] & 0x7F) << 24)
            | (digest[offset + 1] << 16)
            | (digest[offset + 2] << 8)
            | digest[offset + 3];

        return (binaryCode % 1_000_000).ToString(
            "D6",
            CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset TimeAtStep(
        long timeStep,
        int secondsIntoStep = 0)
    {
        return DateTimeOffset.FromUnixTimeSeconds(
            (timeStep * 30) + secondsIntoStep);
    }
}

internal sealed class AdjustableTimeProvider(DateTimeOffset utcNow)
    : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}
