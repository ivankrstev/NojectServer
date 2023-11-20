using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace NojectServer.OptionsSetup
{
    public class JwtBearerOptionsSetup
    {
        public void GetOptions(IConfiguration _config, JwtBearerOptions options)
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:AccessToken"]!)),
                ValidateLifetime = true,
                ValidateIssuer = false,
                ValidateAudience = false
            };
        }
    }
}