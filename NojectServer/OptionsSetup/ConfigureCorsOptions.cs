using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace NojectServer.OptionsSetup;

public class ConfigureCorsOptions(IConfiguration configuration) : IConfigureOptions<CorsOptions>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(CorsOptions options)
    {
        // Retrieve the allowed origins from configuration
        var origins = _configuration["Cors:Origins"]?.Split(",") ?? [];
        options.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins(origins)
                .AllowCredentials();
        });
    }
}