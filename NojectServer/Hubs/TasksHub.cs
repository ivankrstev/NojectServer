using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NojectServer.Data;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Hubs
{
    [Authorize]
    public class TasksHub : Hub
    {
        private readonly DataContext _dataContext;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public TasksHub(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

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

        public async Task<object> AddTask(string projectId, int? prevTaskId = null)
        {
            IDbContextTransaction? transaction = null;
            try
            {
                await _semaphore.WaitAsync();
                var user = Context.User?.Identity?.Name!;
                Guid id = new(projectId);
                transaction = await _dataContext.Database.BeginTransactionAsync();
                var project = await _dataContext.Projects
                    .Where(p => p.Id == id).FirstAsync();
                var maxId = await _dataContext.Tasks
                    .Where(t => t.ProjectId == id)
                    .Select(t => (int?)t.Id)
                    .MaxAsync() ?? 0;
                var prevTask = prevTaskId != null ? await _dataContext.Tasks.Where(t => t.Id == prevTaskId && t.ProjectId == id).FirstOrDefaultAsync() : null;
                Models.Task task = new()
                {
                    Id = maxId + 1,
                    ProjectId = id,
                    CreatedBy = user
                };
                await _dataContext.AddAsync(task);
                project.FirstTask ??= task.Id;
                // Change prev, next task pointers
                task.Next = prevTask?.Next;
                if (prevTask != null) prevTask!.Next = task.Id;
                else
                {
                    var lastTask = await _dataContext.Tasks.Where(t => t.Id != task.Id && t.Next == null && t.ProjectId == id).FirstOrDefaultAsync();
                    if (lastTask != null) lastTask!.Next = task.Id;
                }
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await Clients.OthersInGroup(projectId).SendAsync("AddedTask", new { task });
                return new { task };
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw new HubException($"Error adding task to project {projectId}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<object> ChangeValue(string projectId, int taskId, string newValue)
        {
            try
            {
                var user = Context.User?.Identity?.Name!;
                Guid id = new(projectId);
                var task = await _dataContext.Tasks
                    .Where(t => t.ProjectId == id && t.Id == taskId).FirstOrDefaultAsync() ?? throw new HubException($"Error changing value of task {taskId} of project {projectId}");
                task.Value = newValue;
                task.LastModifiedOn = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
                await Clients.OthersInGroup(projectId).SendAsync("ChangedValue", new { task = new { id = taskId, newValue } });
                return new { task = new { id = taskId, newValue } };
            }
            catch (Exception)
            {
                throw new HubException($"Error changing value of task {taskId} of project {projectId}");
            }
        }
    }
}