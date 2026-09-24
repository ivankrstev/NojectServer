using FluentValidation;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Validates requests for a new email-verification link.
/// </summary>
internal sealed class RequestEmailVerificationInputValidator
    : AbstractValidator<RequestEmailVerificationInput>
{
    public RequestEmailVerificationInputValidator()
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
