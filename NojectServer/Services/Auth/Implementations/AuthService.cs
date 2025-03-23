using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Implementations;

/// <summary>
/// Implementation of the IAuthService interface that handles user authentication,
/// registration, email verification, and password reset functionality.
///
/// This service interacts with the database to manage user accounts and uses
/// auxiliary services for password handling and email communication.
/// </summary>
public class AuthService(
    DataContext dataContext,
    IPasswordService passwordService,
    IEmailService emailService
    ) : IAuthService
{
    /// <summary>
    /// Registers a new user in the system with the provided information.
    /// Creates a user record with a hashed password and sends a verification email.
    /// All operations are executed in a transaction to ensure data consistency.
    /// </summary>
    /// <param name="request">The registration request containing user information</param>
    /// <returns>
    /// A Result containing the created User object if successful,
    /// or an error message if the registration fails
    /// </returns>
    public async Task<Result<User>> RegisterAsync(UserRegisterRequest request)
    {
        if (await dataContext.Users.AnyAsync(u => u.Email == request.Email))
            return Result.Failure<User>("Conflict", "A user with the provided email already exists.", 409);

        var requestError = request.Validate();
        if (requestError != null)
            return Result.Failure<User>(requestError.Error, requestError.Message, 400);

        passwordService.CreatePasswordHash(request.Password, out var passwordHash, out var passwordSalt);
        var user = new User
        {
            Email = request.Email,
            Password = passwordHash,
            PasswordSalt = passwordSalt,
            FullName = request.FullName,
            VerificationToken = TokenGenerator.GenerateRandomToken()
        };
        await using var transaction = await dataContext.Database.BeginTransactionAsync();
        try
        {
            dataContext.Users.Add(user);
            await dataContext.SaveChangesAsync();
            await emailService.SendVerificationLinkAsync(user);
            await transaction.CommitAsync();
            return Result.Success(user);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Failure<User>("ServerError", $"An error occurred during registration: {ex.Message}", 500);
            // or throw; maybe?
        }
    }

    /// <summary>
    /// Authenticates a user based on email and password credentials.
    /// Verifies that the user exists, the password is correct, and the email is verified.
    /// </summary>
    /// <param name="request">The login request containing user credentials</param>
    /// <returns>
    /// A Result containing the user's email if login is successful,
    /// or an error message if authentication fails
    /// </returns>
    public async Task<Result<string>> LoginAsync(UserLoginRequest request)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !passwordService.VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            return Result.Failure<string>("Unauthorized", "Invalid credentials.", 401);

        if (user.VerifiedAt == null)
            return Result.Failure<string>("Unauthorized", "Email not verified.", 401);

        return Result.Success(user.Email);
    }

    /// <summary>
    /// Verifies a user's email address using the verification token sent during registration.
    /// Updates the user record to mark the email as verified with the current timestamp.
    /// </summary>
    /// <param name="email">The email address to verify</param>
    /// <param name="token">The verification token</param>
    /// <returns>
    /// A Result containing a success message if verification is successful,
    /// or an error message if verification fails
    /// </returns>
    public async Task<Result<string>> VerifyEmailAsync(string email, string token)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.VerificationToken == token);
        if (user == null)
            return Result.Failure<string>("NotFound", "Invalid verification information.", 404);

        if (user.VerifiedAt != null)
            return Result.Failure<string>("Conflict", "Email already verified.", 409);

        user.VerifiedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync();
        return Result.Success("Email successfully verified.");
    }

    /// <summary>
    /// Initiates the password reset process for a user by generating a reset token
    /// and sending a password reset email. The token expires after one hour.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset</param>
    /// <returns>
    /// A Result containing a success message if the reset email was sent,
    /// or an error message if the process fails
    /// </returns>
    public async Task<Result<string>> ForgotPasswordAsync(string email)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Failure<string>("NotFound", "User not found.", 404);

        user.PasswordResetToken = TokenGenerator.GenerateRandomToken();
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        await dataContext.SaveChangesAsync();

        try
        {
            await emailService.SendResetPasswordLinkAsync(user);
            return Result.Success("Reset link was sent to your email.");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("ServerError", $"Failed to send reset email: {ex.Message}", 500);
        }
    }
}
