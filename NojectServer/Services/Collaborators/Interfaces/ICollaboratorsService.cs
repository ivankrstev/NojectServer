using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Collaborators.Interfaces;

/// <summary>
/// Provides functionality for managing project collaborators.
/// Handles adding, removing, searching, and retrieving collaborators for projects.
/// Uses the Result pattern for consistent error handling across the application.
/// </summary>
public interface ICollaboratorsService
{
    /// <summary>
    /// Adds a user as a collaborator to a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToAdd">The email of the user to add as collaborator.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> AddCollaboratorAsync(Guid projectId, string userEmailToAdd);

    /// <summary>
    /// Searches for potential collaborators by partial email match.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToFind">The email prefix to search for.</param>
    /// <returns>A Result containing a list of matching user emails.</returns>
    Task<Result<List<string>>> SearchCollaboratorsAsync(Guid projectId, string userEmailToFind);

    /// <summary>
    /// Retrieves all collaborators for a specific project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <returns>A Result containing a list of collaborator emails.</returns>
    Task<Result<List<string>>> GetAllCollaboratorsAsync(Guid projectId);

    /// <summary>
    /// Removes a collaborator from a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="userEmailToRemove">The email of the collaborator to remove.</param>
    /// <returns>A Result containing a success message or failure details.</returns>
    Task<Result<string>> RemoveCollaboratorAsync(Guid projectId, string userEmailToRemove);
}
