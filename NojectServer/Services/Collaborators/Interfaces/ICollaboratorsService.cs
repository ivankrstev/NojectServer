using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Collaborators.Interfaces;

public interface ICollaboratorsService
{
    Task<Result<string>> AddCollaboratorAsync(Guid projectId, string userEmailToAdd, string projectOwnerEmail);

    Task<Result<List<string>>> SearchCollaboratorsAsync(Guid projectId, string userToFind, string projectOwnerEmail);

    Task<Result<List<string>>> GetAllCollaboratorsAsync(Guid projectId);

    Task<Result<string>> RemoveCollaboratorAsync(Guid projectId, string userToRemove);
}
