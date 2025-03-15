using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Services.Collaborators.Interfaces;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Collaborators.Implementations;

public class CollaboratorsService(DataContext dataContext, IHubContext<SharedProjectsHub> hubContext) : ICollaboratorsService
{
    private readonly DataContext _dataContext = dataContext;
    private readonly IHubContext<SharedProjectsHub> _hubContext = hubContext;

    public async Task<Result<string>> AddCollaboratorAsync(Guid projectId, string userEmailToAdd, string projectOwnerEmail)
    {
        if (userEmailToAdd == projectOwnerEmail)
        {
            return Result.Failure<string>("ValidationError", "You cannot add yourself as collaborator");
        }

        if (!await _dataContext.Users.AnyAsync(u => u.Email == userEmailToAdd))
        {
            return Result.Failure<string>("NotFound", "The specified user doesn't exist", 404);
        }

        if (await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userEmailToAdd))
        {
            return Result.Failure<string>("Conflict", "This collaborator is already associated with this project", 409);
        }

        await _dataContext.AddAsync(new Collaborator { ProjectId = projectId, CollaboratorId = userEmailToAdd });
        await _dataContext.SaveChangesAsync();

        var project = await _dataContext.Projects.Where(p => p.Id == projectId).FirstOrDefaultAsync();

        await _hubContext.Clients.Groups(userEmailToAdd).SendAsync(
            "NewSharedProject",
            new { message = $"You were added as collaborator to the project {projectId}" },
            new { project });

        return Result.Success($"Successfully added {userEmailToAdd} as a collaborator");
    }

    public async Task<Result<List<string>>> SearchCollaboratorsAsync(Guid projectId, string userToFind, string projectOwnerEmail)
    {
        var query = from user in _dataContext.Users
                    join collaborator in _dataContext.Collaborators
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

    public async Task<Result<List<string>>> GetAllCollaboratorsAsync(Guid projectId)
    {
        var collaborators = await _dataContext.Collaborators
            .Where(c => c.ProjectId == projectId)
            .Select(c => c.CollaboratorId)
            .ToListAsync();
        return Result.Success(collaborators);
    }

    public async Task<Result<string>> RemoveCollaboratorAsync(Guid projectId, string userToRemove)
    {
        var deletedRows = await _dataContext.Collaborators
            .Where(c => c.ProjectId == projectId && c.CollaboratorId == userToRemove)
            .ExecuteDeleteAsync();

        if (deletedRows == 0)
        {
            return Result.Failure<string>("NotFound", "The specified collaborator doesn't exist", 404);
        }

        await _hubContext.Clients.Group(userToRemove).SendAsync(
            "RemovedSharedProject",
            new { message = $"You were removed as collaborator from the project {projectId}" },
            new { idToDelete = projectId });

        return Result.Success($"Successfully removed {userToRemove} as a collaborator");
    }
}
