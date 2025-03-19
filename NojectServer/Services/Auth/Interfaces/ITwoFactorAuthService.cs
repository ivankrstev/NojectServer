using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Interfaces;

public interface ITwoFactorAuthService
{
    Task<Result<TwoFactorSetupResult>> GenerateSetupCodeAsync(string email);

    Task<Result<string>> EnableTwoFactorAsync(string email, string code);

    Task<Result<string>> DisableTwoFactorAsync(string email, string code);

    Task<Result<bool>> ValidateTwoFactorCodeAsync(string email, string code);
}

public class TwoFactorSetupResult
{
    public string ManualKey { get; set; } = string.Empty;
    public string QrCodeImageUrl { get; set; } = string.Empty;
}