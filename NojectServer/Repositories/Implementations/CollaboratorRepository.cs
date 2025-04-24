using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.Implementations;

public class CollaboratorRepository(DataContext dataContext) : GenericRepository<Collaborator>(dataContext), ICollaboratorRepository
{
    public async Task<IEnumerable<Collaborator>> GetByProjectIdAsync(Guid projectId)
    {
        return await _dbSet.Where(c => c.ProjectId == projectId).ToListAsync();
    }

    public async Task<IEnumerable<Collaborator>> GetByUserEmailAsync(string collaboratorEmail)
    {
        return await _dbSet.Where(c => c.CollaboratorId == collaboratorEmail).ToListAsync();
    }
}
