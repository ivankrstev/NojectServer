using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Interfaces;

/// <summary>
/// Defines the contract for authentication services in the application.
/// This interface provides methods for user registration, login, email verification,
/// and password reset functionality.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user in the system with the provided information.
    /// This method creates a new user account, generates a verification token,
    /// and sends a verification email to the user.
    /// </summary>
    /// <param name="request">The registration request containing user information</param>
    /// <returns>
    /// A Result containing the created User object if successful,
    /// or an error message if the registration fails
    /// </returns>
    Task<Result<User>> RegisterAsync(UserRegisterRequest request);

    /// <summary>
    /// Authenticates a user based on email and password credentials.
    /// This method verifies the credentials and ensures the user's email is verified.
    /// </summary>
    /// <param name="request">The login request containing user credentials</param>
    /// <returns>
    /// A Result containing the user's email if login is successful,
    /// or an error message if authentication fails
    /// </returns>
    Task<Result<string>> LoginAsync(UserLoginRequest request);

    /// <summary>
    /// Verifies a user's email address using the verification token sent during registration.
    /// </summary>
    /// <param name="email">The email address to verify</param>
    /// <param name="token">The verification token</param>
    /// <returns>
    /// A Result containing a success message if verification is successful,
    /// or an error message if verification fails
    /// </returns>
    Task<Result<string>> VerifyEmailAsync(string email, string token);

    /// <summary>
    /// Initiates the password reset process for a user by generating a reset token
    /// and sending a password reset email.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset</param>
    /// <returns>
    /// A Result containing a success message if the reset email was sent,
    /// or an error message if the process fails
    /// </returns>
    Task<Result<string>> ForgotPasswordAsync(string email);
}
