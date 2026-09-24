using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Email;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> errors = [];

        ValidateEmailAddress(options.EmailId, errors);

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            errors.Add($"{EmailOptions.SectionName}:Name is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            errors.Add($"{EmailOptions.SectionName}:Host is required.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            errors.Add($"{EmailOptions.SectionName}:UserName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            errors.Add($"{EmailOptions.SectionName}:Password is required.");
        }

        if (options.Port is < 1 or > 65535)
        {
            errors.Add($"{EmailOptions.SectionName}:Port must be between 1 and 65535.");
        }

        if (options.UseSsl is null)
        {
            errors.Add($"{EmailOptions.SectionName}:UseSsl must be configured.");
        }

        ValidateClientUrl(options.ClientUrl, errors);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateEmailAddress(
        string emailAddress,
        List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        const string configurationPath = $"{EmailOptions.SectionName}:EmailId";

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            errors.Add($"{configurationPath} is required.");
            return;
        }

        string normalizedEmailAddress = emailAddress.Trim();

        if (!MailAddress.TryCreate(
                normalizedEmailAddress,
                out MailAddress? parsedAddress) ||
            !string.Equals(
                parsedAddress.Address,
                normalizedEmailAddress,
                StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{configurationPath} must be a valid email address.");
        }
    }

    private static void ValidateClientUrl(
        string clientUrl,
        List<string> errors)
    {
        const string configurationPath = $"{EmailOptions.SectionName}:ClientUrl";

        if (string.IsNullOrWhiteSpace(clientUrl))
        {
            errors.Add($"{configurationPath} is required.");
            return;
        }

        if (!Uri.TryCreate(
                clientUrl,
                UriKind.Absolute,
                out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"{configurationPath} must be an absolute HTTP or HTTPS URL.");
        }
    }
}
