using FluentValidation;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Validates the input for registering a new user account.
/// </summary>
internal sealed class RegisterInputValidator : AbstractValidator<RegisterInput>
{
    public RegisterInputValidator()
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

        RuleFor(static input => input.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(User.MaximumFullNameLength)
            .WithMessage(
                $"Full name cannot exceed {User.MaximumFullNameLength} characters.");

        RuleFor(static input => input.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage(
                $"Password must contain at least {PasswordPolicy.MinimumLength} characters.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .WithMessage(
                $"Password cannot exceed {PasswordPolicy.MaximumLength} characters.");

        RuleFor(input => input.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(input => input.Password)
            .WithMessage("Passwords do not match.");
    }
}
