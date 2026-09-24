using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NojectServer.Utils;

namespace NojectServer.UnitTests.Api;

public sealed class UserClaimsExtensionsTests
{
    [Fact]
    public void GetUserId_WithSubjectClaim_ReturnsUserId()
    {
        Guid expectedUserId = Guid.NewGuid();
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(JwtRegisteredClaimNames.Sub, expectedUserId.ToString("D")));

        Guid result = user.GetUserId();

        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public void GetUserId_WithoutSubjectClaim_UsesNameIdentifierClaim()
    {
        Guid expectedUserId = Guid.NewGuid();
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString("D")));

        Guid result = user.GetUserId();

        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public void GetUserId_WhenBothClaimsExist_PrefersSubjectClaim()
    {
        Guid subjectUserId = Guid.NewGuid();
        Guid nameIdentifierUserId = Guid.NewGuid();
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                nameIdentifierUserId.ToString("D")),
            new Claim(
                JwtRegisteredClaimNames.Sub,
                subjectUserId.ToString("D")));

        Guid result = user.GetUserId();

        Assert.Equal(subjectUserId, result);
    }

    [Fact]
    public void GetUserId_WithInvalidSubjectClaim_DoesNotUseNameIdentifierFallback()
    {
        Guid nameIdentifierUserId = Guid.NewGuid();
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"),
            new Claim(
                ClaimTypes.NameIdentifier,
                nameIdentifierUserId.ToString("D")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => user.GetUserId());

        Assert.Equal("User ID claim is missing or invalid", exception.Message);
    }

    [Fact]
    public void GetUserId_WithMissingClaims_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => user.GetUserId());

        Assert.Equal("User ID claim is missing or invalid", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-guid")]
    public void GetUserId_WithInvalidSubjectClaim_ThrowsInvalidOperationException(
        string claimValue)
    {
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(JwtRegisteredClaimNames.Sub, claimValue));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => user.GetUserId());

        Assert.Equal("User ID claim is missing or invalid", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-guid")]
    public void GetUserId_WithInvalidNameIdentifierClaim_ThrowsInvalidOperationException(
        string claimValue)
    {
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, claimValue));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => user.GetUserId());

        Assert.Equal("User ID claim is missing or invalid", exception.Message);
    }

    [Fact]
    public void GetUserId_WithNullPrincipal_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserClaimsExtensions.GetUserId(null!));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
}
