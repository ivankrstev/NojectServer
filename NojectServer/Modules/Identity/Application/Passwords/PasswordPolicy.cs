namespace NojectServer.Modules.Identity.Application.Passwords;

/// <summary>
/// Password length constraints enforced across the identity module.
/// </summary>
internal static class PasswordPolicy
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 128;
}
