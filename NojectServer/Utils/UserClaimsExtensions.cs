using System.Security.Claims;

namespace NojectServer.Utils;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            throw new InvalidOperationException("User ID claim is missing or invalid");
        }

        return userId;
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(emailClaim))
        {
            throw new InvalidOperationException("User email claim is missing");
        }

        return emailClaim;
    }
}
