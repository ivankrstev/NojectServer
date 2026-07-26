using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

internal sealed class RequestPasswordResetInputValidator
    : AbstractValidator<RequestPasswordResetInput>
{
    private const int MaximumEmailLength = 254;

    public RequestPasswordResetInputValidator()
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
