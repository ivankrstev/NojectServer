using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken, (Guid UserId, string Token)>
{
    Task<RefreshToken?> GetByUserIdAndTokenAsync(Guid userId, string token);
    Task<RefreshToken?> GetByTokenAsync(string token);
}
