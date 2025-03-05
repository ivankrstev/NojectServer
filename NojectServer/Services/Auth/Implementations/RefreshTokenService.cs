using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Services.Auth.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Auth.Implementations;

public class RefreshTokenService(DataContext dataContext, ITokenService tokenService) : IRefreshTokenService
{
    public async Task<string> GenerateRefreshTokenAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        var refreshToken = new RefreshToken
        {
            Email = email,
            Token = tokenService.CreateRefreshToken(email),
            ExpireDate = DateTime.UtcNow.AddDays(14)
        };
        dataContext.RefreshTokens.Add(refreshToken);
        await dataContext.SaveChangesAsync();
        return refreshToken.Token;
    }

    public async Task<RefreshToken> ValidateRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var refreshToken = await dataContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken == null || refreshToken.ExpireDate < DateTime.UtcNow)
            throw new SecurityTokenException("Invalid or expired refresh token.");
        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var refreshToken = await dataContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            dataContext.RefreshTokens.Remove(refreshToken);
            await dataContext.SaveChangesAsync();
        }
    }
}