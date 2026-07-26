using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.Register;

internal sealed class RegisterInputValidator : AbstractValidator<RegisterInput>
{
    private const int MaximumEmailLength = 254;
    private const int MaximumFullNameLength = 50;
    private const int MinimumPasswordLength = 15;
    private const int MaximumPasswordLength = 128;

    public RegisterInputValidator()
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

        RuleFor(static input => input.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(MaximumFullNameLength)
            .WithMessage(
                $"Full name cannot exceed {MaximumFullNameLength} characters.");

        RuleFor(static input => input.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(MinimumPasswordLength)
            .WithMessage(
                $"Password must contain at least {MinimumPasswordLength} characters.")
            .MaximumLength(MaximumPasswordLength)
            .WithMessage(
                $"Password cannot exceed {MaximumPasswordLength} characters.");

        RuleFor(input => input.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(input => input.Password)
            .WithMessage("Passwords do not match.");
    }
}
