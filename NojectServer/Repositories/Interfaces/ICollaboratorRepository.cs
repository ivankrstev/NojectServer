using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface ICollaboratorRepository : IGenericRepository<Collaborator>
{
    Task<IEnumerable<Collaborator>> GetByProjectIdAsync(Guid projectId);
    Task<IEnumerable<Collaborator>> GetByUserEmailAsync(string collaboratorEmail);
}
