using NojectServer.Configurations.Email;
using NojectServer.Configurations.Tokens;
using NojectServer.Configurations.Totp;

namespace NojectServer.DependencyInjection;

/// <summary>
/// Provides service collection extensions for application dependency configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and validates application configuration options at startup.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration source.</param>
    public static void AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure application options from configuration
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // Bind and validate shared JWT issuer and audience settings at startup.
        services
            .AddOptionsWithValidateOnStart<
                JwtOptions,
                JwtOptionsValidator>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName));

        // Bind and validate refresh-token lifetime settings at startup.
        services
            .AddOptionsWithValidateOnStart<
                RefreshTokenOptions,
                RefreshTokenOptionsValidator>()
            .Bind(configuration.GetRequiredSection(RefreshTokenOptions.SectionName));

        // Bind and validate access-token signing and expiration settings at startup.
        services
            .AddOptionsWithValidateOnStart<
                AccessTokenOptions,
                AccessTokenOptionsValidator>()
            .Bind(configuration.GetRequiredSection(AccessTokenOptions.SectionName));

        // Bind and validate TFA-token signing and expiration settings at startup.
        services
            .AddOptionsWithValidateOnStart<
                TfaTokenOptions,
                TfaTokenOptionsValidator>()
            .Bind(configuration.GetRequiredSection(TfaTokenOptions.SectionName));

        // Bind and validate email settings during application startup.
        services
            .AddOptionsWithValidateOnStart<
                EmailOptions,
                EmailOptionsValidator>()
            .Bind(configuration.GetRequiredSection(EmailOptions.SectionName));

        // Bind and validate TOTP settings during application startup.
        services
            .AddOptionsWithValidateOnStart<
                TotpOptions,
                TotpOptionsValidator>()
            .Bind(configuration.GetRequiredSection(TotpOptions.SectionName));
    }
}
