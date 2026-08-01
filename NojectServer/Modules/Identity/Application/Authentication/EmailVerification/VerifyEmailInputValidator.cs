using FluentValidation;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Validates email-verification input.
/// </summary>
internal sealed class VerifyEmailInputValidator
    : AbstractValidator<VerifyEmailInput>
{
    private const int MaximumTokenLength = 128;

    public VerifyEmailInputValidator()
    {
        RuleFor(static input => input.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(User.MaximumEmailLength)
            .WithMessage(
                $"Email cannot exceed {User.MaximumEmailLength} characters.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(static input => input.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MaximumLength(MaximumTokenLength)
            .WithMessage(
                $"Token cannot exceed {MaximumTokenLength} characters.");
    }
}
