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

    public async Task<IEnumerable<Project>> GetByUserEmailAsync(string userEmail)
    {
        return await _dbSet.Where(p => p.CreatedBy == userEmail).ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetCollaboratorProjectsAsync(string userEmail)
    {
        return await _dbSet
            .Where(p => _dataContext.Collaborators.Any(c => c.ProjectId == p.Id && c.CollaboratorId == userEmail))
            .ToListAsync();
    }

    public async Task<bool> HasUserAccessToProjectAsync(Guid projectId, string userEmail)
    {
        return await _dbSet.AnyAsync(p => p.Id == projectId && p.CreatedBy == userEmail) ||
       await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userEmail);
    }

    public async Task<bool> IsUserProjectOwnerAsync(Guid projectId, string userEmail)
    {
        return await _dbSet.AnyAsync(p => p.Id == projectId && p.CreatedBy == userEmail);
    }
}
