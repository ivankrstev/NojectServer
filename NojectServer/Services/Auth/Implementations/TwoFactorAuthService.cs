using NojectServer.Models;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Auth.Validation.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Implementations;

/// <summary>
/// Implementation of the ITwoFactorAuthService interface that manages two-factor
/// authentication (2FA) functionality for the application.
///
/// This service handles the generation of 2FA setup codes, enabling/disabling 2FA
/// for user accounts, and validating TOTP codes during the authentication process.
/// It utilizes TOTP (Time-based One-Time Password) standards to generate and validate
/// verification codes.
/// </summary>
public class TwoFactorAuthService(
    IConfiguration config,
    IUnitOfWork unitOfWork,
    ITotpValidator totpValidator) : ITwoFactorAuthService
{
    private readonly IConfiguration _config = config;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITotpValidator _totpValidator = totpValidator;
    private readonly IUserRepository _userRepository = unitOfWork.GetRepository<User>() as IUserRepository
        ?? throw new InvalidOperationException("Failed to get user repository");

    /// <summary>
    /// Disables two-factor authentication for a user after verifying the provided code.
    /// This method validates the code using the TOTP validator before disabling 2FA
    /// and removing the secret key from the user's account.
    /// </summary>
    /// <param name="userId"> The ID of the user to disable 2FA for</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a success message if 2FA was disabled successfully,
    /// or an error message with appropriate status code if the operation fails
    /// </returns>
    public async Task<Result<string>> DisableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _userRepository.GetByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        if (!user.TwoFactorEnabled)
            return Result.Failure<string>("BadRequest", "2FA is already disabled.", 400);

        if (!_totpValidator.ValidateCode(user.TwoFactorSecretKey!, code.Trim()))
            return Result.Failure<string>("Unauthorized", "Invalid security code.", 401);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("2FA disabled successfully.");
    }

    /// <summary>
    /// Enables two-factor authentication for a user after verifying the provided code.
    /// This method checks if the user has a secret key generated and validates the code
    /// using the TOTP validator before enabling 2FA for the user's account.
    /// </summary>
    /// <param name="userId"> The ID of the user to enable 2FA for</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a success message if 2FA was enabled successfully,
    /// or an error message if the operation fails
    /// </returns>
    public async Task<Result<string>> EnableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _userRepository.GetByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Result.Failure<string>("BadRequest", "Generate a setup code first.");

        if (!_totpValidator.ValidateCode(user.TwoFactorSecretKey, code.Trim()))
            return Result.Failure<string>("Unauthorized", "Invalid security code.");

        user.TwoFactorEnabled = true;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("2FA enabled successfully.");
    }

    /// <summary>
    /// Generates a setup code for 2FA and prepares the user account for 2FA enrollment.
    /// This method creates a new secret key and generates a QR code URL that can be
    /// displayed to the user for scanning with a TOTP app.
    /// </summary>
    /// <param name="userId"> The ID of the user to create the setup code for</param>
    /// <returns>
    /// A Result containing TwoFactorSetupResult with the manual key and QR code URL if successful,
    /// or an error message if the operation fails
    /// </returns>
    public async Task<Result<TwoFactorSetupResult>> GenerateSetupCodeAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure<TwoFactorSetupResult>("NotFound", "User not found.", 404);

        if (user.TwoFactorEnabled)
            return Result.Failure<TwoFactorSetupResult>("BadRequest", "2FA is already enabled.", 400);

        // Generate a new secret key
        var secretKey = TokenGenerator.GenerateRandomToken(32);
        var appName = _config["AppName"] ?? "Noject";

        // Create QR code URL
        string qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={secretKey}&issuer={Uri.EscapeDataString(appName)}&digits=6&period=30";

        // Save the secret key to the user
        user.TwoFactorSecretKey = secretKey;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(new TwoFactorSetupResult
        {
            ManualKey = secretKey,
            QrCodeImageUrl = qrCodeUrl
        });
    }

    /// <summary>
    /// Validates a TOTP code provided by the user during the login process.
    /// This method checks if the user has 2FA enabled and validates the code
    /// using the TOTP validator.
    /// </summary>
    /// <param name="userId"> The ID of the user to validate the code for</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a boolean indicating whether the code is valid,
    /// or an error message if the validation process fails
    /// </returns>
    public async Task<Result<bool>> ValidateTwoFactorCodeAsync(Guid userId, string code)
    {
        var user = await _userRepository.GetByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure<bool>("NotFound", "User not found.", 404);
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Result.Failure<bool>("BadRequest", "Two-factor authentication is not set up.", 400);

        var isValid = _totpValidator.ValidateCode(user.TwoFactorSecretKey, code.Trim());
        return Result.Success(isValid);
    }
}
