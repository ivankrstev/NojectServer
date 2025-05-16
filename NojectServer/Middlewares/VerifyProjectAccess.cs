using Microsoft.AspNetCore.Mvc.Filters;
using NojectServer.Repositories.Interfaces;
using System.Security.Claims;

namespace NojectServer.Middlewares;

[AttributeUsage(AttributeTargets.Method)]
public class VerifyProjectAccess(IProjectRepository projectRepository) : Attribute, IAsyncResourceFilter
{
    private readonly IProjectRepository _projectRepository = projectRepository;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        // Check if the userId is valid and can be parsed to a Guid
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid parsedUserId))
        {
            context.HttpContext.Response.StatusCode = 401;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "User ID is not valid"
            });
            return;
        }

        // Check if the project ID is present in the route data and can be parsed to a Guid
        if (context.RouteData.Values.TryGetValue("id", out var projectIdParam) && Guid.TryParse(projectIdParam!.ToString(), out Guid parsedProjectId))
        {
            if (!await _projectRepository.HasUserAccessToProjectAsync(parsedProjectId, parsedUserId))
            {
                context.HttpContext.Response.StatusCode = 403;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Access denied",
                    message = "You do not have permission to access this project"
                });
                return;
            }

            await next();
            return;
        }

        context.HttpContext.Response.StatusCode = 400;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Invalid project id",
            message = "The provided project id is not a valid GUID"
        });
        return;
    }
}
