using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Models.Requests.Projects;
using NojectServer.Repositories.Interfaces;
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
    private readonly IProjectRepository _projectRepository = unitOfWork.GetRepository<Project>() as IProjectRepository
        ?? throw new InvalidOperationException("Failed to get project repository");
    private readonly ITaskRepository _taskRepository = unitOfWork.GetRepository<Models.Task>() as ITaskRepository
        ?? throw new InvalidOperationException("Failed to get task repository");
    private readonly ICollaboratorRepository _collaboratorRepository = unitOfWork.GetRepository<Models.Task>() as ICollaboratorRepository
    ?? throw new InvalidOperationException("Failed to get task repository");

    /// <summary>
    /// Creates a new project with generated colors.
    /// </summary>
    /// <param name="request">The project creation request containing the project name.</param>
    /// <param name="createdBy">The email of the user creating the project.</param>
    /// <returns>A Result containing the created project.</returns>
    public async Task<Result<Project>> CreateProjectAsync(CreateUpdateProjectRequest request, string createdBy)
    {
        GenerateColors(out string color, out string backgroundColor);
        Project project = new()
        {
            Name = request.Name,
            BackgroundColor = backgroundColor,
            Color = color,
            CreatedBy = createdBy
        };

        await _unitOfWork.GetRepository<Project>().AddAsync(project);
        await _projectRepository.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(project);
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project to delete.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    /// Task: Implement notification to collaborators about project deletion(update their UI properly).
    public async Task<Result<string>> DeleteProjectAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        _unitOfWork.GetRepository<Project>().Remove(project);
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
    /// <param name="userEmail">The email of the user.</param>
    /// <returns>A Result containing a list of projects owned by the user.</returns>
    public async Task<Result<List<Project>>> GetOwnProjectsAsync(string userEmail)
    {
        var projects = await _unitOfWork.GetRepository<Project>().FindAsync(p => p.CreatedBy == userEmail);

        return Result.Success(projects.ToList());
    }

    /// <summary>
    /// Gets all projects shared with a user.
    /// </summary>
    /// <param name="userEmail">The email of the user.</param>
    /// <returns>A Result containing a list of projects shared with the user.</returns>
    public async Task<Result<List<Project>>> GetProjectsAsCollaboratorAsync(string userEmail)
    {
        List<Project> sharedProjects = await _projectRepository.Query()
            .Join(
                _collaboratorRepository.Query(),
                p => p.Id,
                c => c.ProjectId,
                (p, c) => new { Project = p, Collaborator = c })
            .Where(joinedResult => joinedResult.Collaborator.CollaboratorId == userEmail)
            .Select(joinedResult => joinedResult.Project)
            .ToListAsync();

        return Result.Success(sharedProjects);
    }

    /// <summary>
    /// Gets a shared project's tasks.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing an array of tasks or failure details.</returns>
    public async Task<Result<List<Models.Task>>> GetTasksAsCollaboratorAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        if (project == null)
        {
            return Result.Failure<List<Models.Task>>(
                "AccessDenied",
                "You do not have permission to access this project.",
                403);
        }
        int? first_task = project.FirstTask;

        var tasks = await _taskRepository.FindAsync(t => t.ProjectId == projectId);
        var taskArray = tasks.ToArray();

        taskArray.OrderTasks(first_task);

        return Result.Success(tasks.ToList());
    }

    /// <summary>
    /// Grants public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> GrantPublicAccessAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already public.", 400);
        }

        project.IsPublic = true;
        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("Project sharing is enabled.");
    }

    /// <summary>
    /// Revokes public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> RevokePublicAccessAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (!project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already private.", 400);
        }

        project.IsPublic = false;
        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success("Project sharing is disabled.");
    }

    /// <summary>
    /// Updates a project's name.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="request">The update request containing the new project name.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> UpdateProjectNameAsync(Guid projectId, CreateUpdateProjectRequest request)
    {
        var project = await _projectRepository.GetByIdAsync(projectId.ToString());

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        project.Name = request.Name;
        _projectRepository.Update(project);
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
