using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.VerifyEmail;

internal sealed class VerifyEmailInputValidator : AbstractValidator<VerifyEmailInput>
{
    private const int MaximumEmailLength = 254;
    private const int MaximumTokenLength = 128;

    public VerifyEmailInputValidator()
    {
        RuleFor(static input => input.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(MaximumEmailLength)
            .WithMessage(
                $"Email cannot exceed {MaximumEmailLength} characters.")
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
