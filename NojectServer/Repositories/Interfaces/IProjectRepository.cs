using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<IEnumerable<Project>> GetByUserEmailAsync(string userEmail);
    Task<IEnumerable<Project>> GetCollaboratorProjectsAsync(string userEmail);
    Task<bool> IsUserProjectOwnerAsync(Guid projectId, string userEmail);
    Task<bool> HasUserAccessToProjectAsync(Guid projectId, string userEmail);
}
