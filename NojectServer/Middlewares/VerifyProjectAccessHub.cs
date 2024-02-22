using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using System.Security.Claims;

namespace NojectServer.Middlewares
{
    public class VerifyProjectAccessHub : IHubFilter
    {
        private readonly DataContext _dataContext;
        private readonly List<string> _methodsRequiringMiddleware = new() { "ProjectJoin", "AddTask", "ChangeValue" };
        public static readonly SemaphoreSlim _semaphore = new(90, 90); // Count of concurrent users that can use task operations, so requests can wait if max pool is on limit

        public VerifyProjectAccessHub(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

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
                if (!await CheckProjectAccess(projectId, invocationContext.Context.User?.FindFirst(ClaimTypes.Name)?.Value!))
                    throw new HubException("You do not have permission to access this project");
            }
            // Calling the actual hub method
            return await next(invocationContext);
        }

        private async Task<bool> CheckProjectAccess(Guid projectId, string userEmail)
        {
            try
            {
                await _semaphore.WaitAsync();
                return await _dataContext.Projects.AnyAsync(p => p.Id == projectId && p.CreatedBy == userEmail) ||
                        await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == projectId && c.CollaboratorId == userEmail);
            }
            catch (Exception)
            {
                _semaphore.Release();
                throw new HubException("Unable to verify your access to the requested project");
            }
        }
    }
}