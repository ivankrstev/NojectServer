using FluentValidation;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Validates pending-account email changes.
/// </summary>
internal sealed class ChangePendingEmailInputValidator
    : AbstractValidator<ChangePendingEmailInput>
{
    public ChangePendingEmailInputValidator()
    {
        RuleFor(static input => input.NewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("New email is required.")
            .MaximumLength(User.MaximumEmailLength)
            .WithMessage(
                $"New email cannot exceed {User.MaximumEmailLength} characters.")
            .EmailAddress()
            .WithMessage("New email must be a valid email address.");
    }
}
