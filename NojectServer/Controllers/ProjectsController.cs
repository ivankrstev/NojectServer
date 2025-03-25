using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Middlewares;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Services.Projects.Interfaces;
using NojectServer.Utils.ResultPattern;
using System.Security.Claims;

namespace NojectServer.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class ProjectsController(IProjectsService projectsService) : ControllerBase
{
    private readonly IProjectsService _projectsService = projectsService;

    [HttpPost("", Name = "Create a Project")]
    public async Task<ActionResult<Project>> Create(AddUpdateProjectRequest request)
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _projectsService.CreateProjectAsync(request, userEmail);

        return result switch
        {
            SuccessResult<Project> success => Created(nameof(Project), success.Value),
            FailureResult<Project> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpGet("", Name = "Get all own projects")]
    public async Task<ActionResult<List<Project>>> GetOwnProjects()
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _projectsService.GetOwnProjectsAsync(userEmail);

        return result switch
        {
            SuccessResult<List<Project>> success => Ok(new { projects = success.Value }),
            FailureResult<List<Project>> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpGet("shared", Name = "Get all shared projects")]
    public async Task<ActionResult<List<Project>>> GetSharedProjects()
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _projectsService.GetProjectsAsCollaboratorAsync(userEmail);

        return result switch
        {
            SuccessResult<List<Project>> success => Ok(new { sharedProjects = success.Value }),
            FailureResult<List<Project>> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<ActionResult> UpdateProjectName(Guid id, AddUpdateProjectRequest request)
    {
        var result = await _projectsService.UpdateProjectNameAsync(id, request);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpDelete("{id}", Name = "DeleteById")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _projectsService.DeleteProjectAsync(id);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPut("{id}/share", Name = "ToggleProjectSharingOn")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> ToggleSharingOn(Guid id)
    {
        var result = await _projectsService.GrantPublicAccessAsync(id);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpDelete("{id}/share", Name = "ToggleProjectSharingOff")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> ToggleSharingOff(Guid id)
    {
        var result = await _projectsService.RevokePublicAccessAsync(id);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpGet("{id}/share", Name = "GetPublicProjectTasks ")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Models.Task>>> GetPublicProjectTasks(Guid id)
    {
        var result = await _projectsService.GetTasksAsCollaboratorAsync(id);

        return result switch
        {
            SuccessResult<List<Models.Task>> success => Ok(new { tasks = success.Value }),
            FailureResult<List<Models.Task>> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}
