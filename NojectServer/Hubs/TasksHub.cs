using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NojectServer.Hubs
{
    [Authorize]
    public class TasksHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("ConnectionInit", "Successfully connected to tasks hub");
        }

        public async Task<string> ProjectJoin(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
            return $"Joined the {projectId} project group";
        }

        public async Task<string> ProjectLeave(string projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
            return $"Left the {projectId} project group";
        }
    }
}