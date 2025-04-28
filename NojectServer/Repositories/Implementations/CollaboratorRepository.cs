using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.Implementations;

public class CollaboratorRepository(DataContext dataContext) : GenericRepository<Collaborator>(dataContext), ICollaboratorRepository
{
    public async Task<IEnumerable<Collaborator>> GetCollaboratorsByProjectIdAsync(Guid projectId)
    {
        return await _dbSet.Where(c => c.ProjectId == projectId).ToListAsync();
    }
}
