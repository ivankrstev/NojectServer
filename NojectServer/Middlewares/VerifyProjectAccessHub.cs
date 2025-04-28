using Microsoft.AspNetCore.SignalR;
using NojectServer.Repositories.Interfaces;
using System.Security.Claims;

namespace NojectServer.Middlewares;

public class VerifyProjectAccessHub(IProjectRepository projectRepository) : IHubFilter
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly List<string> _methodsRequiringMiddleware = ["ProjectJoin", "AddTask", "ChangeValue", "DeleteTask", "CompleteTask", "UncompleteTask", "IncreaseLevel", "DecreaseLevel"];
    public static readonly SemaphoreSlim _semaphore = new(90, 90); // Count of concurrent users that can use task operations, so requests can wait if max pool is on limit

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Before the method execution
        if (_methodsRequiringMiddleware.Contains(invocationContext.HubMethodName))
        {
            var arguments = invocationContext.HubMethodArguments;
            if (!Guid.TryParse(arguments[0] as string, out var projectId))
                throw new HubException("The provided project id is not a valid GUID");

            // Check if the user can access the project with id = projectId
            var userId = invocationContext.Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
                throw new HubException("User ID is not valid");
            if (!await CheckProjectAccess(projectId, parsedUserId))
                throw new HubException("You do not have permission to access this project");
        }

        // Calling the actual hub method
        return await next(invocationContext);
    }

    private async Task<bool> CheckProjectAccess(Guid projectId, Guid userId)
    {
        try
        {
            await _semaphore.WaitAsync();
            return await _projectRepository.HasUserAccessToProjectAsync(projectId, userId);
        }
        catch (Exception)
        {
            _semaphore.Release();
            throw new HubException("Unable to verify your access to the requested project");
        }
    }
}
