using FluentValidation;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Validates password-reset request input.
/// </summary>
internal sealed class RequestPasswordResetInputValidator
    : AbstractValidator<RequestPasswordResetInput>
{
    public RequestPasswordResetInputValidator()
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
    }
}
