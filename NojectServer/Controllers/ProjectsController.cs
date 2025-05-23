﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Middlewares;
using NojectServer.Models;
using NojectServer.Models.Requests.Projects;
using NojectServer.Services.Projects.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class ProjectsController(IProjectsService projectsService) : ControllerBase
{
    private readonly IProjectsService _projectsService = projectsService;

    [HttpPost("", Name = "Create a Project")]
    public async Task<ActionResult<Project>> Create(CreateUpdateProjectRequest request)
    {
        var userId = User.GetUserId();
        var result = await _projectsService.CreateProjectAsync(request, userId);

        return result.ToActionResult(this, project => Created(nameof(project), project));
    }

    [HttpGet("", Name = "Get all own projects")]
    public async Task<IActionResult> GetOwnProjects()
    {
        var userId = User.GetUserId();
        var result = await _projectsService.GetOwnProjectsAsync(userId);

        return result.ToActionResult(this, projects => Ok(new { projects }));
    }

    [HttpGet("shared", Name = "Get all shared projects")]
    public async Task<IActionResult> GetSharedProjects()
    {
        var userId = User.GetUserId();
        var result = await _projectsService.GetProjectsAsCollaboratorAsync(userId);

        return result.ToActionResult(this, sharedProjects => Ok(new { sharedProjects }));
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> UpdateProjectName(Guid id, CreateUpdateProjectRequest request)
    {
        var result = await _projectsService.UpdateProjectNameAsync(id, request);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpDelete("{id}", Name = "DeleteById")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _projectsService.DeleteProjectAsync(id);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpPut("{id}/share", Name = "ToggleProjectSharingOn")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> ToggleSharingOn(Guid id)
    {
        var result = await _projectsService.GrantPublicAccessAsync(id);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpDelete("{id}/share", Name = "ToggleProjectSharingOff")]
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public async Task<IActionResult> ToggleSharingOff(Guid id)
    {
        var result = await _projectsService.RevokePublicAccessAsync(id);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpGet("{id}/share", Name = "GetPublicProjectTasks ")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProjectTasks(Guid id)
    {
        var result = await _projectsService.GetTasksAsCollaboratorAsync(id);

        return result.ToActionResult(this, tasks => Ok(new { tasks }));
    }
}
