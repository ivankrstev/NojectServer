using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Models.Requests;
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
public class ProjectsService(DataContext dataContext, IHubContext<SharedProjectsHub> hubContext) : IProjectsService
{
    private readonly DataContext _dataContext = dataContext;
    private readonly IHubContext<SharedProjectsHub> _hubContext = hubContext;

    /// <summary>
    /// Creates a new project with generated colors.
    /// </summary>
    /// <param name="request">The project creation request containing the project name.</param>
    /// <param name="createdBy">The email of the user creating the project.</param>
    /// <returns>A Result containing the created project.</returns>
    public async Task<Result<Project>> CreateProjectAsync(AddUpdateProjectRequest request, string createdBy)
    {
        GenerateColors(out string color, out string backgroundColor);
        Project project = new()
        {
            Name = request.Name,
            BackgroundColor = backgroundColor,
            Color = color,
            CreatedBy = createdBy
        };

        _dataContext.Add(project);
        await _dataContext.SaveChangesAsync();

        return Result.Success(project);
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project to delete.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> DeleteProjectAsync(Guid projectId)
    {
        var deletedRows = await _dataContext.Projects
            .Where(p => p.Id == projectId)
            .ExecuteDeleteAsync();

        if (deletedRows == 0)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        return Result.Success($"Project with ID {projectId} successfully deleted.");
    }

    /// <summary>
    /// Gets all projects owned by a user.
    /// </summary>
    /// <param name="userEmail">The email of the user.</param>
    /// <returns>A Result containing a list of projects owned by the user.</returns>
    public async Task<Result<List<Project>>> GetOwnProjectsAsync(string userEmail)
    {
        List<Project> projects = await _dataContext.Projects
            .Where(p => p.CreatedBy == userEmail)
            .ToListAsync();

        return Result.Success(projects);
    }

    /// <summary>
    /// Gets all projects shared with a user.
    /// </summary>
    /// <param name="userEmail">The email of the user.</param>
    /// <returns>A Result containing a list of projects shared with the user.</returns>
    public async Task<Result<List<Project>>> GetProjectsAsCollaboratorAsync(string userEmail)
    {
        List<Project> sharedProjects = await _dataContext.Projects
            .Join(
                _dataContext.Collaborators,
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
        var project = await _dataContext.Projects.Where(p => p.Id == projectId && p.IsPublic).FirstOrDefaultAsync();

        if (project == null)
        {
            return Result.Failure<List<Models.Task>>(
                "AccessDenied",
                "You do not have permission to access this project.",
                403);
        }

        int? first_task = await _dataContext.Projects
            .Where(p => p.Id == projectId)
            .AsNoTracking()
            .Select(p => p.FirstTask)
            .FirstOrDefaultAsync();

        var tasks = await _dataContext.Tasks
            .Where(t => t.ProjectId == projectId)
            .AsNoTrackingWithIdentityResolution()
            .ToArrayAsync();

        tasks.OrderTasks(first_task);

        return Result.Success(tasks.ToList());
    }

    /// <summary>
    /// Grants public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> GrantPublicAccessAsync(Guid projectId)
    {
        var project = await _dataContext.Projects.Where(p => p.Id == projectId).FirstOrDefaultAsync();

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already public.", 400);
        }

        project.IsPublic = true;
        await _dataContext.SaveChangesAsync();

        return Result.Success("Project sharing is enabled.");
    }

    /// <summary>
    /// Revokes public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> RevokePublicAccessAsync(Guid projectId)
    {
        var project = await _dataContext.Projects.Where(p => p.Id == projectId).FirstOrDefaultAsync();

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        if (!project.IsPublic)
        {
            return Result.Failure<string>("BadRequest", "Project is already private.", 400);
        }

        project.IsPublic = false;
        await _dataContext.SaveChangesAsync();

        return Result.Success("Project sharing is disabled.");
    }

    /// <summary>
    /// Updates a project's name.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="request">The update request containing the new project name.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    public async Task<Result<string>> UpdateProjectNameAsync(Guid projectId, AddUpdateProjectRequest request)
    {
        var project = await _dataContext.Projects.Where(p => p.Id == projectId).SingleOrDefaultAsync();

        if (project == null)
        {
            return Result.Failure<string>("NotFound", "Project not found.", 404);
        }

        project.Name = request.Name;
        await _dataContext.SaveChangesAsync();

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
        for (int i = 0; i < 6; i++) backgroundColor += letters[new Random().Next(0, 16)];
        int red = int.Parse(backgroundColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
        int green = int.Parse(backgroundColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
        int blue = int.Parse(backgroundColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
        int yiq = (red * 299 + green * 587 + blue * 114) / 1000;
        color = yiq >= 128 ? "#000" : "#FFF";
    }
}
