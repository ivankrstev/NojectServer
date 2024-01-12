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

        public async Task ProjectJoin(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
            await Clients.Caller.SendAsync($"Joined the {projectId} project group");
        }

        public async Task ProjectLeave(string projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
            await Clients.Caller.SendAsync($"Left the {projectId} project group");
        }
    }
}