namespace NojectServer.Modules.Identity.Api.Contracts.Authentication;

public sealed class RegisterRequest
{
    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required string Password { get; init; }

    public required string ConfirmPassword { get; init; }
}
