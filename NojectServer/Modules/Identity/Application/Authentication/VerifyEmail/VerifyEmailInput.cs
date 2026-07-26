namespace NojectServer.Modules.Identity.Application.Authentication.VerifyEmail;

public sealed record VerifyEmailInput(
    string Email,
    string Token);
