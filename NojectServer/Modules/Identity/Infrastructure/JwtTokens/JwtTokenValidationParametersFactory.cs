using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Configurations.Tokens;

namespace NojectServer.Modules.Identity.Infrastructure.JwtTokens;

public sealed class JwtTokenValidationParametersFactory(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AccessTokenOptions> accessTokenOptions,
    IOptions<TfaTokenOptions> tfaTokenOptions,
    IOptions<PendingEmailVerificationTokenOptions> pendingEmailVerificationTokenOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly AccessTokenOptions _accessTokenOptions =
        accessTokenOptions.Value;
    private readonly TfaTokenOptions _tfaTokenOptions =
        tfaTokenOptions.Value;
    private readonly PendingEmailVerificationTokenOptions _pendingEmailVerificationTokenOptions =
        pendingEmailVerificationTokenOptions.Value;

    public TokenValidationParameters CreateAccessTokenParameters()
    {
        return Create(_accessTokenOptions.SecretKey);
    }

    public TokenValidationParameters CreateTfaTokenParameters()
    {
        return Create(_tfaTokenOptions.SecretKey);
    }

    public TokenValidationParameters CreatePendingEmailVerificationTokenParameters()
    {
        return Create(_pendingEmailVerificationTokenOptions.SecretKey);
    }

    private TokenValidationParameters Create(string secretKey)
    {
        return new()
        {
            // Signature
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(secretKey)),
            ValidAlgorithms =
            [
                SecurityAlgorithms.HmacSha256,
            ],

            // Issuer 
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,

            // Audience
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,

            // Lifetime
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewInSeconds!.Value)
        };
    }
}
