using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Hubs;
using System.Security.Claims;

namespace NojectServer.Middlewares
{
    public class VerifyProjectAccessHub : IHubFilter
    {
        private readonly IHubContext<TasksHub> _hubContext;
        private readonly DataContext _dataContext;

        public VerifyProjectAccessHub(IHubContext<TasksHub> hubContext, DataContext dataContext)
        {
            _hubContext = hubContext;
            _dataContext = dataContext;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            // Before the method execution
            if (invocationContext.HubMethodName == "ProjectJoin")
            {
                var arguments = invocationContext.HubMethodArguments;
                var projectIdArgument = arguments.First() as string;
                var userConnId = invocationContext.Context.ConnectionId;
                // Check if the user can access the project with id = firstArgument
                if (!await CheckProjectAccess(projectIdArgument!, invocationContext.Context.User?.FindFirst(ClaimTypes.Name)?.Value!))
                {
                    await _hubContext.Clients.Clients(userConnId).SendAsync("AccessDenied", "You do not have permission to access this project");
                    return Task.CompletedTask;
                }
            }
            // Calling the actual hub method
            return await next(invocationContext);
        }

        private async Task<bool> CheckProjectAccess(string projectId, string userEmail)
        {
            bool isAllowed = await _dataContext.Projects.AnyAsync(p => p.Id == new Guid(projectId) && p.CreatedBy == userEmail) ||
                await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == new Guid(projectId) && c.CollaboratorId == userEmail);
            return isAllowed;
        }
    }
}