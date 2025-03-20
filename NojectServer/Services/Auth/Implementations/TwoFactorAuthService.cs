using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Auth.Validation.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Implementations;

public class TwoFactorAuthService(
    IConfiguration config,
    DataContext dataContext,
    ITotpValidator totpValidator) : ITwoFactorAuthService
{
    private readonly IConfiguration _config = config;
    private readonly DataContext _dataContext = dataContext;
    private readonly ITotpValidator _totpValidator = totpValidator;

    public async Task<Result<string>> DisableTwoFactorAsync(string email, string code)
    {
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        if (!user.TwoFactorEnabled)
            return Result.Failure<string>("BadRequest", "2FA is already disabled.", 400);

        if (!_totpValidator.ValidateCode(user.TwoFactorSecretKey!, code.Trim()))
            return Result.Failure<string>("Unauthorized", "Invalid security code.", 401);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        await _dataContext.SaveChangesAsync();

        return Result.Success("2FA disabled successfully.");
    }

    public async Task<Result<string>> EnableTwoFactorAsync(string email, string code)
    {
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Result.Failure<string>("BadRequest", "Generate a setup code first.");

        if (!_totpValidator.ValidateCode(user.TwoFactorSecretKey, code.Trim()))
            return Result.Failure<string>("Unauthorized", "Invalid security code.");

        user.TwoFactorEnabled = true;
        await _dataContext.SaveChangesAsync();

        return Result.Success("2FA enabled successfully.");
    }

    public async Task<Result<TwoFactorSetupResult>> GenerateSetupCodeAsync(string email)
    {
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<TwoFactorSetupResult>("NotFound", "User not found.", 404);

        if (user.TwoFactorEnabled)
            return Result.Failure<TwoFactorSetupResult>("BadRequest", "2FA is already enabled.", 400);

        // Generate a new secret key
        var secretKey = TokenGenerator.GenerateRandomToken(32);
        var appName = _config["AppName"] ?? "Noject";

        // Create QR code URL
        string qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Uri.EscapeDataString(appName)}&digits=6&period=30";

        // Save the secret key to the user
        user.TwoFactorSecretKey = secretKey;
        await _dataContext.SaveChangesAsync();

        return Result.Success(new TwoFactorSetupResult
        {
            ManualKey = secretKey,
            QrCodeImageUrl = qrCodeUrl
        });
    }

    public async Task<Result<bool>> ValidateTwoFactorCodeAsync(string email, string code)
    {
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<bool>("NotFound", "User not found.", 404);
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Result.Failure<bool>("BadRequest", "Two-factor authentication is not set up.", 400);

        var isValid = _totpValidator.ValidateCode(user.TwoFactorSecretKey, code.Trim());
        return Result.Success(isValid);
    }
}
