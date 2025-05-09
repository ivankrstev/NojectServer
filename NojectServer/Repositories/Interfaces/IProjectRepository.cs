using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface IProjectRepository : IGenericRepository<Project, Guid>
{
    Task<IEnumerable<Project>> GetByCreatorEmailAsync(string userEmail);
    Task<bool> IsUserProjectOwnerAsync(Guid projectId, Guid userId);
    Task<bool> HasUserAccessToProjectAsync(Guid projectId, Guid userId);
}
