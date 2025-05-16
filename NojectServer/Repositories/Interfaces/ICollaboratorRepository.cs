using NojectServer.Models;
using NojectServer.Repositories.Base;

namespace NojectServer.Repositories.Interfaces;

public interface ICollaboratorRepository : IGenericRepository<Collaborator, (Guid ProjectId, Guid CollaboratorId)>
{
    Task<IEnumerable<Collaborator>> GetCollaboratorsByProjectIdAsync(Guid projectId);
}
