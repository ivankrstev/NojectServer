using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Validates password-reset completion input.
/// </summary>
internal sealed class ResetPasswordInputValidator
    : AbstractValidator<ResetPasswordInput>
{
    private const int MaximumTokenLength = 128;
    private const int MinimumPasswordLength = 15;
    private const int MaximumPasswordLength = 128;

    public ResetPasswordInputValidator()
    {
        RuleFor(static input => input.ResetToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Reset token is required.")
            .MaximumLength(MaximumTokenLength)
            .WithMessage(
                $"Reset token cannot exceed {MaximumTokenLength} characters.");

        RuleFor(static input => input.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(MinimumPasswordLength)
            .WithMessage(
                $"New password must contain at least {MinimumPasswordLength} characters.")
            .MaximumLength(MaximumPasswordLength)
            .WithMessage(
                $"New password cannot exceed {MaximumPasswordLength} characters.");

        RuleFor(static input => input.ConfirmNewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("New password confirmation is required.")
            .Equal(static input => input.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}
