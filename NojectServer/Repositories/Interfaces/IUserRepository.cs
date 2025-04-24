using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
