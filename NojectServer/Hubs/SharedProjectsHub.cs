using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NojectServer.Hubs
{
    [Authorize]
    public sealed class SharedProjectsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User?.FindFirst(ClaimTypes.Name)?.Value!;
            await Groups.AddToGroupAsync(Context.ConnectionId, user);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User?.FindFirst(ClaimTypes.Name)?.Value!;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, user);
            await base.OnDisconnectedAsync(exception);
        }
    }
}