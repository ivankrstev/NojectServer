using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Collaborators.Interfaces;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Collaborators.Implementations;

/// <summary>
/// Implementation of the ICollaboratorsService interface that manages project collaborations.
///
/// This service handles all operations related to project collaborators, including adding,
/// removing, searching for, and retrieving collaborators. It maintains the relationships between
/// users and projects they can access, and sends real-time events via SignalR when
/// collaboration status changes, allowing immediate UI updates for affected users.
/// </summary>
public class CollaboratorsService(IUnitOfWork unitOfWork, IHubContext<SharedProjectsHub> hubContext) : ICollaboratorsService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<SharedProjectsHub> _hubContext = hubContext;
    private readonly IUserRepository _userRepository = unitOfWork.GetRepository<User>() as IUserRepository
        ?? throw new InvalidOperationException("Failed to get user repository");
    private readonly ICollaboratorRepository _collaboratorRepository = unitOfWork.GetRepository<Collaborator>() as ICollaboratorRepository
        ?? throw new InvalidOperationException("Failed to get collaborator repository");
    private readonly IProjectRepository _projectRepository = unitOfWork.GetRepository<Project>() as IProjectRepository
        ?? throw new InvalidOperationException("Failed to get project repository");

    /// <summary>
    /// Adds a user as a collaborator to a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToAdd">The email of the user to add as collaborator.</param>
    /// <param name="projectOwnerEmail">The email of the project owner.</param>
    /// <returns>
    /// A Result containing a success message if the user was added successfully,
    /// or a failure with appropriate error message and status code.
    /// </returns>
    /// <remarks>
    /// Validates that:
    /// - User isn't adding themselves
    /// - User to add exists in the system
    /// - User isn't already a collaborator
    /// Sends a real-time event via SignalR to the added user, enabling immediate UI update
    /// with the newly shared project.
    /// </remarks>
    public async Task<Result<string>> AddCollaboratorAsync(Guid projectId, string userEmailToAdd, string projectOwnerEmail)
    {
        if (userEmailToAdd == projectOwnerEmail)
        {
            return Result.Failure<string>("ValidationError", "You cannot add yourself as collaborator");
        }

        if (!await _userRepository.AnyAsync(u => u.Email == userEmailToAdd))
        {
            return Result.Failure<string>("NotFound", "The specified user doesn't exist", 404);
        }

        if (await _collaboratorRepository.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userEmailToAdd))
        {
            return Result.Failure<string>("Conflict", "This collaborator is already associated with this project", 409);
        }

        var collaborator = new Collaborator { ProjectId = projectId, CollaboratorId = userEmailToAdd };
        await _collaboratorRepository.AddAsync(collaborator);
        await _unitOfWork.SaveChangesAsync();

        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        await _hubContext.Clients.Groups(userEmailToAdd).SendAsync(
            "NewSharedProject",
            new { message = $"You were added as collaborator to the project {projectId}" },
            new { project });

        return Result.Success($"Successfully added {userEmailToAdd} as a collaborator");
    }

    /// <summary>
    /// Searches for potential collaborators by partial email match.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userToFind">The email prefix to search for.</param>
    /// <param name="projectOwnerEmail">The email of the project owner to exclude from results.</param>
    /// <returns>
    /// A Result containing a list of user emails that match the search criteria and
    /// are not already collaborators on the project.
    /// </returns>
    /// <remarks>
    /// The search excludes:
    /// - Users who are already collaborators on the project
    /// - The project owner
    /// - Users whose email doesn't start with the search term
    /// </remarks>
    public async Task<Result<List<string>>> SearchCollaboratorsAsync(Guid projectId, string userToFind, string projectOwnerEmail)
    {
        var query = from user in _userRepository.Query()
                    join collaborator in _collaboratorRepository.Query()
                    on new { UserId = user.Email, ProjectId = projectId } equals new
                    {
                        UserId = collaborator.CollaboratorId,
                        collaborator.ProjectId
                    } into gj
                    from subCollaborator in gj.DefaultIfEmpty()
                    where subCollaborator == null && user.Email != projectOwnerEmail && user.Email.StartsWith(userToFind)
                    select user.Email;
        var result = await query.ToListAsync();
        return Result.Success(result);
    }

    /// <summary>
    /// Retrieves all collaborators for a specific project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a list of collaborator emails for the project.</returns>
    public async Task<Result<List<string>>> GetAllCollaboratorsAsync(Guid projectId)
    {
        var collaborators = await _collaboratorRepository.FindAsync(c => c.ProjectId == projectId);
        var collaboratorIds = collaborators.Select(c => c.CollaboratorId).ToList();
        return Result.Success(collaboratorIds);
    }

    /// <summary>
    /// Removes a collaborator from a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userToRemove">The email of the collaborator to remove.</param>
    /// <returns>
    /// A Result containing a success message if the collaborator was removed successfully,
    /// or a failure with appropriate error message and status code.
    /// </returns>
    /// <remarks>
    /// Sends a real-time event via SignalR to the removed collaborator, enabling immediate
    /// removal of the project from their UI without requiring a page refresh.
    /// </remarks>
    public async Task<Result<string>> RemoveCollaboratorAsync(Guid projectId, string userToRemove)
    {
        var collaborators = await _collaboratorRepository.FindAsync(
            c => c.ProjectId == projectId && c.CollaboratorId == userToRemove);

        var collaborator = collaborators.FirstOrDefault();
        if (collaborator == null)
        {
            return Result.Failure<string>("NotFound", "The specified collaborator doesn't exist", 404);
        }

        _collaboratorRepository.Remove(collaborator);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.Group(userToRemove).SendAsync(
            "RemovedSharedProject",
            new { message = $"You were removed as collaborator from the project {projectId}" },
            new { idToDelete = projectId });

        return Result.Success($"Successfully removed {userToRemove} as a collaborator");
    }
}
