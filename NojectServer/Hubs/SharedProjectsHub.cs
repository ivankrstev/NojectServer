using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Security.Claims;

namespace NojectServer.Hubs
{
    [Authorize]
    public sealed class SharedProjectsHub : Hub
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public SharedProjectsHub(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User?.FindFirst(ClaimTypes.Name)?.Value!;
            await _connectionMultiplexer.GetDatabase().SetAddAsync("sharedprojects:" + user, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User?.FindFirst(ClaimTypes.Name)?.Value!;
            await _connectionMultiplexer.GetDatabase().SetRemoveAsync("sharedprojects:" + user, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}