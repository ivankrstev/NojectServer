using FluentValidation;
using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Validates the input for completing a two-factor login.
/// </summary>
internal sealed class CompleteTwoFactorLoginInputValidator : AbstractValidator<CompleteTwoFactorLoginInput>
{
    private const int MaximumTfaTokenLength = 128;

    public CompleteTwoFactorLoginInputValidator()
    {
        RuleFor(static input => input.TfaToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tfa token is required.")
            .MaximumLength(MaximumTfaTokenLength)
            .WithMessage(
                $"Tfa token cannot exceed {MaximumTfaTokenLength} characters.");

        RuleFor(static input => input.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Code is required.")
            .Length(OtpNetTotpService.CodeSizeInDigits)
            .WithMessage(
                $"Code must be exactly {OtpNetTotpService.CodeSizeInDigits} characters.");
    }
}
