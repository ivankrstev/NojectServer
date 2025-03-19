using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Interfaces;

public interface IAuthService
{
    Task<Result<User>> RegisterAsync(UserRegisterRequest request);

    Task<Result<string>> LoginAsync(UserLoginRequest request);

    Task<Result<string>> VerifyEmailAsync(string email, string token);

    Task<Result<string>> ForgotPasswordAsync(string email);
}
