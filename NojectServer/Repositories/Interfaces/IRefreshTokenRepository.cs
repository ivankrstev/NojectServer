using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByEmailAndTokenAsync(string email, string token);
    Task<RefreshToken?> GetByTokenAsync(string token);
}
