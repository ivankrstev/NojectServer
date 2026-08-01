using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Validates the input for performing a login.
/// </summary>
internal sealed class LoginInputValidator : AbstractValidator<LoginInput>
{
    private const int MaximumEmailLength = 254;
    private const int MaximumPasswordLength = 128;

    public LoginInputValidator()
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

        RuleFor(static input => input.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(MaximumPasswordLength)
            .WithMessage(
                $"Password cannot exceed {MaximumPasswordLength} characters.");
    }
}
