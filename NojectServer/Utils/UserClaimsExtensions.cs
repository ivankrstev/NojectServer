using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NojectServer.Utils;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string? userIdClaim =
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            throw new InvalidOperationException("User ID claim is missing or invalid");
        }

        return userId;
    }
}
