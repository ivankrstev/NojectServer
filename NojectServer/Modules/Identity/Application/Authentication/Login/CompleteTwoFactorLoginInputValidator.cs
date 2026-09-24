using FluentValidation;
using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Validates the input for completing a two-factor login.
/// </summary>
internal sealed class CompleteTwoFactorLoginInputValidator : AbstractValidator<CompleteTwoFactorLoginInput>
{
    // Sized for a base64url-encoded JWT (header.payload.signature). A lean HS256 token
    // with a few claims runs ~200-330 chars; RS256 (2048-bit) pushes the signature
    // segment alone to 342. Verify against a real token from your issuer and adjust.
    private const int MaximumTfaTokenLength = 512;
    private const string JwtFormatPattern = @"^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$";

    public CompleteTwoFactorLoginInputValidator()
    {
        RuleFor(static input => input.TfaToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tfa token is required.")
            .MaximumLength(MaximumTfaTokenLength)
            .WithMessage($"Tfa token cannot exceed {MaximumTfaTokenLength} characters.")
            .Matches(JwtFormatPattern)
            .WithMessage("Tfa token is not a well-formed token.");

        RuleFor(static input => input.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Code is required.")
            .Length(OtpNetTotpService.CodeSizeInDigits)
            .WithMessage(
                $"Code must be exactly {OtpNetTotpService.CodeSizeInDigits} characters.")
            .Matches(@"^[0-9]+$")
            .WithMessage("Code must contain only digits.");
    }
}
