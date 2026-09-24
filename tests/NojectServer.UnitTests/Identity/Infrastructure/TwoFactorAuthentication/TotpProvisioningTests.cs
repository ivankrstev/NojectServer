using System.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace NojectServer.UnitTests.Identity.Infrastructure.TwoFactorAuthentication;

public sealed class TotpProvisioningTests
{
    [Fact]
    public void GenerateSecret_ReturnsTwentyRandomBytes()
    {
        var service = TotpTestSupport.CreateService(TimeProvider.System);

        byte[] firstSecret = service.GenerateSecret();
        byte[] secondSecret = service.GenerateSecret();

        Assert.Equal(20, firstSecret.Length);
        Assert.Equal(20, secondSecret.Length);
        Assert.NotEqual(
            Convert.ToHexString(firstSecret),
            Convert.ToHexString(secondSecret));
    }

    [Fact]
    public void EncodeSecret_ReturnsUnpaddedBase32()
    {
        var service = TotpTestSupport.CreateService(TimeProvider.System);

        string encodedSecret = service.EncodeSecret(
            TotpTestSupport.CreateKnownSecret());

        Assert.Equal(
            "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            encodedSecret);
    }

    [Fact]
    public void EncodeSecret_WithNullSecret_ThrowsArgumentNullException()
    {
        var service = TotpTestSupport.CreateService(TimeProvider.System);

        Assert.Throws<ArgumentNullException>(() => service.EncodeSecret(null!));
    }

    [Fact]
    public void EncodeSecret_WithEmptySecret_ThrowsArgumentException()
    {
        var service = TotpTestSupport.CreateService(TimeProvider.System);

        Assert.Throws<ArgumentException>(() => service.EncodeSecret([]));
    }

    [Fact]
    public void CreateProvisioningUri_ContainsExpectedParametersAndTrimmedLabel()
    {
        var service = TotpTestSupport.CreateService(
            TimeProvider.System,
            issuer: " Example Issuer ");

        string uri = service.CreateProvisioningUri(
            TotpTestSupport.CreateKnownSecret(),
            " alice@example.com ");

        Assert.Equal(
            "otpauth://totp/Example%20Issuer:alice%40example.com?"
            + "secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&"
            + "issuer=Example%20Issuer&algorithm=SHA1&digits=6&period=30",
            uri);
    }

    [Fact]
    public void CreateProvisioningUri_EscapesIssuerAccountAndQueryValues()
    {
        var service = TotpTestSupport.CreateService(
            TimeProvider.System,
            issuer: " Contoso & Co ");

        string uri = service.CreateProvisioningUri(
            TotpTestSupport.CreateKnownSecret(),
            " alice+tag@example.com/region ");
        var parsedUri = new Uri(uri);
        var query = QueryHelpers.ParseQuery(parsedUri.Query);

        Assert.Equal("otpauth", parsedUri.Scheme);
        Assert.Equal("totp", parsedUri.Host);
        Assert.Equal(
            "Contoso & Co:alice+tag@example.com/region",
            Uri.UnescapeDataString(parsedUri.AbsolutePath.TrimStart('/')));
        Assert.Equal(
            "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            query["secret"].Single());
        Assert.Equal("Contoso & Co", query["issuer"].Single());
        Assert.Equal("SHA1", query["algorithm"].Single());
        Assert.Equal("6", query["digits"].Single());
        Assert.Equal("30", query["period"].Single());

        Assert.Contains("Contoso%20%26%20Co", uri);
        Assert.Contains("alice%2Btag%40example.com%2Fregion", uri);
        Assert.DoesNotContain(" ", uri);
    }

    [Fact]
    public void CreateProvisioningUri_WithBlankAccountName_ThrowsArgumentException()
    {
        var service = TotpTestSupport.CreateService(TimeProvider.System);

        Assert.Throws<ArgumentException>(() => service.CreateProvisioningUri(
            TotpTestSupport.CreateKnownSecret(),
            " \t"));
    }
}
