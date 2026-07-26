using FluentValidation;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

internal sealed class CompleteTwoFactorLoginInputValidator : AbstractValidator<CompleteTwoFactorLoginInput>
{
    private const int MaximumTfaTokenLength = 128;
    private const int MaximumCodeLength = 6;

    public CompleteTwoFactorLoginInputValidator()
    {
        RuleFor(static input => input.TfaToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tfa token is required.")
            .MaximumLength(MaximumTfaTokenLength)
            .WithMessage(
                $"Tfa token cannot exceed {MaximumTfaTokenLength} characters.");

        RuleFor(static input => input.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Code is required.")
            .MaximumLength(MaximumCodeLength)
            .WithMessage(
                $"Code cannot exceed {MaximumCodeLength} characters.");
    }
}
