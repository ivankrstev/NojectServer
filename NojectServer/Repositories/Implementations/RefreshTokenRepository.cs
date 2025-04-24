using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.Implementations;

public class RefreshTokenRepository(DataContext dataContext)
    : GenericRepository<RefreshToken>(dataContext), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByEmailAndTokenAsync(string email, string token)
    {
        return await _dbSet.FirstOrDefaultAsync(rt => rt.Email == email && rt.Token == token);
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return _dbSet.FirstOrDefaultAsync(rt => rt.Token == token);
    }
}
