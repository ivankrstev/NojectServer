using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Hubs;
using NojectServer.Models;
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

    /// <summary>
    /// Adds a user as a collaborator to a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToAdd">The email of the user to add as collaborator.</param>
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
    public async Task<Result<string>> AddCollaboratorAsync(Guid projectId, string userEmailToAdd)
    {
        // Check if the project exists
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId.ToString());
        if (project == null)
        {
            return Result.Failure<string>("NotFound", "The specified project doesn't exist", 404);
        }

        // Check if the user to add exists
        var userToAdd = await _unitOfWork.Users.GetByEmailAsync(userEmailToAdd);
        if (userToAdd == null)
        {
            return Result.Failure<string>("NotFound", "The specified user doesn't exist", 404);
        }

        // Check if the user is the project owner
        if (project.CreatedBy == userToAdd.Id)
        {
            return Result.Failure<string>("ValidationError", "You cannot add yourself as collaborator");
        }

        // Check if the user is already a collaborator
        if (await _unitOfWork.Collaborators.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userToAdd.Id))
        {
            return Result.Failure<string>("Conflict", "This collaborator is already associated with this project", 409);
        }

        var collaborator = new Collaborator { ProjectId = projectId, CollaboratorId = userToAdd.Id };
        await _unitOfWork.Collaborators.AddAsync(collaborator);
        await _unitOfWork.SaveChangesAsync();

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
    /// <param name="userEmailToFind">The email prefix to search for.</param>
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
    public async Task<Result<List<string>>> SearchCollaboratorsAsync(Guid projectId, string userEmailToFind)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId.ToString());
        if (project == null)
        {
            return Result.Failure<List<string>>("NotFound", "The specified project doesn't exist", 404);
        }

        var projectOwnerId = project.CreatedBy;

        var query = from user in _unitOfWork.Users.Query()
                    join collaborator in _unitOfWork.Collaborators.Query()
                    on new { UserId = user.Id, ProjectId = projectId } equals new
                    {
                        UserId = collaborator.CollaboratorId,
                        collaborator.ProjectId
                    } into gj
                    from subCollaborator in gj.DefaultIfEmpty()
                    where subCollaborator == null
                        && user.Id != projectOwnerId
                        && user.Email.StartsWith(userEmailToFind)
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
        var collaborators = await _unitOfWork.Collaborators.FindAsync(c => c.ProjectId == projectId);
        var collaboratorIds = collaborators.Select(c => c.CollaboratorId.ToString()).ToList();
        return Result.Success(collaboratorIds);
    }

    /// <summary>
    /// Removes a collaborator from a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToRemove">The email of the collaborator to remove.</param>
    /// <returns>
    /// A Result containing a success message if the collaborator was removed successfully,
    /// or a failure with appropriate error message and status code.
    /// </returns>
    /// <remarks>
    /// Sends a real-time event via SignalR to the removed collaborator, enabling immediate
    /// removal of the project from their UI without requiring a page refresh.
    /// </remarks>
    public async Task<Result<string>> RemoveCollaboratorAsync(Guid projectId, string userEmailToRemove)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(userEmailToRemove);

        if (user == null)
        {
            return Result.Failure<string>("NotFound", "The specified user doesn't exist", 404);
        }

        var collaborators = await _unitOfWork.Collaborators.FindAsync(
            c => c.ProjectId == projectId && c.CollaboratorId == user.Id);

        var collaborator = collaborators.FirstOrDefault();
        if (collaborator == null)
        {
            return Result.Failure<string>("NotFound", "The specified collaborator doesn't exist", 404);
        }

        _unitOfWork.Collaborators.Remove(collaborator);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.Group(userEmailToRemove).SendAsync(
            "RemovedSharedProject",
            new { message = $"You were removed as collaborator from the project {projectId}" },
            new { idToDelete = projectId });

        return Result.Success($"Successfully removed {userEmailToRemove} as a collaborator");
    }
}
