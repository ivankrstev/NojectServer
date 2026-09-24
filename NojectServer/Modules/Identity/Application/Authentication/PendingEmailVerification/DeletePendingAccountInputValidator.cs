using FluentValidation;
using NojectServer.Modules.Identity.Application.Passwords;

namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Validates pending-account deletion requests.
/// </summary>
internal sealed class DeletePendingAccountInputValidator
    : AbstractValidator<DeletePendingAccountInput>
{
    public DeletePendingAccountInputValidator()
    {
        RuleFor(static input => input.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .WithMessage(
                $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }
}
