using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using NojectServer.Services.Auth.Interfaces;

namespace NojectServer.OptionsSetup;

public class JwtBearerOptionsSetup(ITokenService tokenService) : IConfigureOptions<JwtBearerOptions>
{
    private readonly ITokenService _tokenService = tokenService;

    public void Configure(JwtBearerOptions options)
    {
        // Configure the JWT bearer authentication options
        options.TokenValidationParameters = _tokenService.GetAccessTokenValidationParameters();

        // Set the JWT bearer events for message received
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/SharedProjectsHub") || path.StartsWithSegments("/TasksHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    }
}
