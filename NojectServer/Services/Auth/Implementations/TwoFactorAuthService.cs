using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;
using OtpNet;

namespace NojectServer.Services.Auth.Implementations;

public class TwoFactorAuthService(
    IConfiguration config,
    DataContext dataContext
    ) : ITwoFactorAuthService
{
    private readonly IConfiguration _config = config;
    private readonly DataContext _dataContext = dataContext;

    public async Task<Result<string>> DisableTwoFactorAsync(string email, string code)
    {
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        if (!user.TwoFactorEnabled)
            return Result.Failure<string>("BadRequest", "2FA is already disabled.", 400);

        if (!ValidateCode(user.TwoFactorSecretKey!, code.Trim()))
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

        if (!ValidateCode(user.TwoFactorSecretKey, code.Trim()))
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

        var isValid = ValidateCode(user.TwoFactorSecretKey, code.Trim());
        return Result.Success(isValid);
    }

    private static bool ValidateCode(string secretKey, string code)
    {
        try
        {
            // Convert the Base32 secret to byte array
            var secretKeyBytes = Base32Encoding.ToBytes(secretKey);

            // Create TOTP instance with 30-second window
            var totp = new Totp(secretKeyBytes, step: 30);

            // Verify with some time drift allowance (1 period before and after)
            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch
        {
            return false;
        }
    }
}