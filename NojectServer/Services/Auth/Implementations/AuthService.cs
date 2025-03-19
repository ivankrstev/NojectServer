using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Implementations;

public class AuthService(
    DataContext dataContext,
    IPasswordService passwordService,
    IEmailService emailService
    ) : IAuthService
{
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

    public async Task<Result<string>> LoginAsync(UserLoginRequest request)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !passwordService.VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            return Result.Failure<string>("Unauthorized", "Invalid credentials.", 401);

        if (user.VerifiedAt == null)
            return Result.Failure<string>("Unauthorized", "Email not verified.", 401);

        return Result.Success(user.Email);
    }

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