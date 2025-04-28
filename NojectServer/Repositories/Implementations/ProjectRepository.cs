using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.Implementations;

public class ProjectRepository(DataContext dataContext)
    : GenericRepository<Project>(dataContext), IProjectRepository
{
    private new readonly DataContext _dataContext = dataContext;

    public async Task<IEnumerable<Project>> GetByCreatorEmailAsync(string userEmail)
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => _dataContext.Users.Any(u => u.Email == userEmail && u.Id == p.CreatedBy))
            .ToListAsync();
    }

    public async Task<bool> HasUserAccessToProjectAsync(Guid projectId, Guid userId)
    {
        // Checks if user is owner or collaborator
        return await _dbSet.AnyAsync(p => p.Id == projectId && p.CreatedBy == userId) ||
            await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userId);
    }

    public async Task<bool> IsUserProjectOwnerAsync(Guid projectId, Guid userId)
    {
        return await _dbSet.AnyAsync(p => p.Id == projectId && p.CreatedBy == userId);
    }
}
