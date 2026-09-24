using FluentValidation;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Validates the input for performing a login.
/// </summary>
internal sealed class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
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

        RuleFor(static input => input.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .WithMessage(
                $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");
    }
}
