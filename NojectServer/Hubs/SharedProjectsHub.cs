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
            AddNewConnection(user, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User?.FindFirst(ClaimTypes.Name)?.Value!;
            RemoveExistingConnection(user, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async void AddNewConnection(string user, string connId)
        {
            var redisDb = _connectionMultiplexer.GetDatabase();
            await redisDb.SetAddAsync("sharedprojects:" + user, connId);
        }

        public async void RemoveExistingConnection(string user, string connId)
        {
            var redisDb = _connectionMultiplexer.GetDatabase();
            await redisDb.SetRemoveAsync("sharedprojects:" + user, connId);
        }
    }
}