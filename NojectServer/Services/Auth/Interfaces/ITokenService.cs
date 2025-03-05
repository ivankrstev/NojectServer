using Microsoft.IdentityModel.Tokens;

namespace NojectServer.Services.Auth.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(string email);

    string CreateRefreshToken(string email);

    string CreateTfaToken(string email);

    TokenValidationParameters GetTfaTokenValidationParameters();
}