using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface ICollaboratorRepository : IGenericRepository<Collaborator>
{
    Task<IEnumerable<Collaborator>> GetCollaboratorsByProjectIdAsync(Guid projectId);
}
