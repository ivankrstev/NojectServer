using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Models.Requests.Projects;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Projects.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Projects.Implementations;

/// <summary>
/// Implementation of the IProjectsService interface that manages projects.
///
/// This service handles all operations related to projects, including creating,
/// reading, updating, deleting, and sharing projects. It maintains the core project
/// data and provides functionality for project management across the application.
/// </summary>
public class ProjectsService(
    IUnitOfWork unitOfWork,
    IHubContext<SharedProjectsHub> hubContext) : IProjectsService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<SharedProjectsHub> _hubContext = hubContext;

    /// <summary>
    /// Creates a new project with generated colors.
    /// </summary>
    /// <param name="request">The project creation request containing the project name.</param>
    /// <param name="createdBy"> The ID of the user creating the project.</param>
    /// <returns>A Result containing the created project.</returns>
    public async Task<Result<Project>> CreateProjectAsync(CreateUpdateProjectRequest request, Guid createdBy)
    {
        GenerateColors(out string color, out string backgroundColor);
        Project project = new()
        {
            Name = request.Name,
            BackgroundColor = backgroundColor,
            Color = color,
            CreatedBy = createdBy
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(project);
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to delete.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    /// TODO: Implement notification to collaborators about project deletion(update their UI properly).
    public async Task<Result<string>> DeleteProjectAsync(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync();
        // Notify all collaborators about the project deletion
        //var collaborators = await _unitOfWork.GetRepository<Collaborator>().FindAsync(c => c.ProjectId == projectId);
        //foreach (var collaborator in collaborators)
        //{
        //    await _hubContext.Clients.User(collaborator.CollaboratorId).SendAsync("ProjectDeleted", projectId);
        //}

        return Result.Success($"Project with ID {projectId} successfully deleted.");
    }

    /// <summary>
    /// Gets all projects owned by a user.
    /// </summary>
    /// <param name="userId"> The ID of the user whose projects are to be retrieved.</param>
    /// <returns>A Result containing a list of projects owned by the user.</returns>
    public async Task<Result<List<Project>>> GetOwnProjectsAsync(Guid userId)
    {
        var projects = await _unitOfWork.Projects.FindAsync(p => p.CreatedBy == userId);

        return Result.Success(projects.ToList());
    }

    /// <summary>
    /// Gets all projects shared with a user.
    /// </summary>
    /// <param name="userId"> The ID of the user whose shared projects are to be retrieved.</param>
    /// <returns>A Result containing a list of projects shared with the user.</returns>
    public async Task<Result<List<Project>>> GetProjectsAsCollaboratorAsync(Guid userId)
    {
        List<Project> sharedProjects = await _unitOfWork.Projects.Query()
            .Join(
                _unitOfWork.Collaborators.Query(),
                p => p.Id,
                c => c.ProjectId,
                (p, c) => new { Project = p, Collaborator = c })
            .Where(joinedResult => joinedResult.Collaborator.CollaboratorId == userId)
            .Select(joinedResult => joinedResult.Project)
            .ToListAsync();

        return Result.Success(sharedProjects);
    }

    /// <summary>
    /// Gets a shared project's tasks.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve tasks from.</param>
    /// <returns>A Result containing an array of tasks or failure details.</returns>
    public async Task<Result<List<Models.Task>>> GetTasksAsCollaboratorAsync(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

        if (project == null)
        {
            return Result.Failure<List<Models.Task>>(
                "AccessDenied",
                "You do not have permission to access this project.",
                403);
        }
        int? first_task = project.FirstTask;

        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.ProjectId == projectId);
        var taskArray = tasks.ToArray();

        taskArray.OrderTasks(first_task);

        return Result.Success(tasks.ToList());
    }

    /// <summary>
    /// Grants public access for a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to grant access to.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> GrantPublicAccessAsync(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already public.", 400);
        }

        project.IsPublic = true;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("Project sharing is enabled.");
    }

    /// <summary>
    /// Revokes public access for a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to revoke access from.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> RevokePublicAccessAsync(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (!project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already private.", 400);
        }

        project.IsPublic = false;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("Project sharing is disabled.");
    }

    /// <summary>
    /// Updates a project's name.
    /// </summary>
    /// <param name="projectId"> The ID of the project to update.</param>
    /// <param name="request">The update request containing the new project name.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> UpdateProjectNameAsync(Guid projectId, CreateUpdateProjectRequest request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        project.Name = request.Name;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success($"Project with ID {projectId} successfully updated.");
    }

    /// <summary>
    /// Generates contrasting foreground and background colors for a project.
    /// </summary>
    /// <param name="color">Output parameter for the foreground color.</param>
    /// <param name="backgroundColor">Output parameter for the background color.</param>
    private static void GenerateColors(out string color, out string backgroundColor)
    {
        string letters = "0123456789ABCDEF";
        backgroundColor = "#";
        for (int i = 0; i < 6; i++) backgroundColor += letters[Random.Shared.Next(0, 16)];
        // Use TryParse instead of Parse for better error handling
        if (!int.TryParse(backgroundColor.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out int red) ||
            !int.TryParse(backgroundColor.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out int green) ||
            !int.TryParse(backgroundColor.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out int blue))
        {
            // Handle parsing error - provide default values
            red = 0;
            green = 0;
            blue = 0;
        }
        int yiq = (red * 299 + green * 587 + blue * 114) / 1000;
        color = yiq >= 128 ? "#000" : "#FFF";
    }
}
