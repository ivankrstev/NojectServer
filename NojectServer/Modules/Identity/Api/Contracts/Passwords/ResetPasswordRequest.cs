namespace NojectServer.Modules.Identity.Api.Contracts.Passwords;

public sealed class ResetPasswordRequest
{
    public required string ResetToken { get; init; }

    public required string NewPassword { get; init; }

    public required string ConfirmNewPassword { get; init; }
}
