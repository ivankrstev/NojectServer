using NojectServer.Models.Requests;
using NojectServer.Models;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Projects.Interfaces;

/// <summary>
/// Provides functionality for managing projects.
/// Handles creating, reading, updating, deleting, and sharing projects.
/// Uses the Result pattern for consistent error handling across the application.
/// </summary>
public interface IProjectsService
{
    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="request">The project creation request containing the project name.</param>
    /// <param name="createdBy">The email of the user creating the project.</param>
    /// <returns>A Result containing the created project or failure details.</returns>
    Task<Result<Project>> CreateProjectAsync(AddUpdateProjectRequest request, string createdBy);

    /// <summary>
    /// Gets all projects owned by a user.
    /// </summary>
    /// <param name="userEmail">The email of the user.</param>
    /// <returns>A Result containing a list of projects owned by the user.</returns>
    Task<Result<List<Project>>> GetOwnProjectsAsync(string userEmail);

    /// <summary>
    /// Retrieves all projects where the specified user has been added as a collaborator.
    /// </summary>
    /// <param name="userEmail">The email of the user to get collaborative projects for.</param>
    /// <returns>A Result containing a list of projects shared with the user.</returns>
    Task<Result<List<Project>>> GetProjectsAsCollaboratorAsync(string userEmail);

    /// <summary>
    /// Updates a project's name.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="request">The update request containing the new project name.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> UpdateProjectNameAsync(Guid projectId, AddUpdateProjectRequest request);

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project to delete.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> DeleteProjectAsync(Guid projectId);

    /// <summary>
    /// Grants public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> GrantPublicAccessAsync(Guid projectId);

    /// <summary>
    /// Revokes public access for a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> RevokePublicAccessAsync(Guid projectId);

    /// <summary>
    /// Retrieves all tasks from a project where the user is a collaborator.
    /// </summary>
    /// <param name="projectId">The unique identifier of the collaborative project.</param>
    /// <returns>A Result containing an array of tasks or failure details.</returns>
    Task<Result<List<Models.Task>>> GetTasksAsCollaboratorAsync(Guid projectId);
}
