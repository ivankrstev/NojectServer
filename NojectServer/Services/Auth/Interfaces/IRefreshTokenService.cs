using NojectServer.Models;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Auth.Interfaces;

public interface IRefreshTokenService
{
    Task<string> GenerateRefreshTokenAsync(string email);

    Task<RefreshToken> ValidateRefreshTokenAsync(string token);

    Task RevokeRefreshTokenAsync(string token);
}