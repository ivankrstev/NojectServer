using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Validates requests for a new email-verification link.
/// </summary>
internal sealed class RequestEmailVerificationInputValidator
    : AbstractValidator<RequestEmailVerificationInput>
{
    private const int MaximumEmailLength = 254;

    public RequestEmailVerificationInputValidator()
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
    }
}
