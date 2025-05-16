using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NojectServer.Middlewares;
using NojectServer.Services.Tasks.Interfaces;
using System.Security.Claims;

namespace NojectServer.Hubs;

[Authorize]
public class TasksHub(ITasksService tasksService) : Hub
{
    private readonly ITasksService _tasksService = tasksService;

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
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!
                ?? throw new HubException("User ID not found in the context.");

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new HubException("Invalid User ID format.");

            Guid id = new(projectId);

            var task = await _tasksService.AddTaskAsync(id, parsedUserId, prevTaskId);

            await Clients.OthersInGroup(projectId).SendAsync("AddedTask", new { task });
            return new { task };
        }
        catch (Exception ex)
        {
            throw new HubException($"Error adding task to Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task<object> ChangeValue(string projectId, int taskId, string newValue)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.ChangeValueAsync(id, taskId, newValue);

            await Clients.OthersInGroup(projectId).SendAsync("ChangedValue",
                new { task = new { id = taskId, newValue } });

            return new { task = new { id = taskId, newValue } };
        }
        catch (Exception ex)
        {
            throw new HubException($"Error changing value of Task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task DeleteTask(string projectId, int taskId)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.DeleteTaskAsync(id, taskId);

            await Clients.OthersInGroup(projectId).SendAsync("DeletedTask",
                new { task = new { id = taskId } });
        }
        catch (Exception ex)
        {
            throw new HubException($"Error deleting Task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task IncreaseLevel(string projectId, int taskId)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.IncreaseTaskLevelAsync(id, taskId);

            await Clients.OthersInGroup(projectId).SendAsync("IncreasedLevel", new { id = taskId });
        }
        catch (Exception ex)
        {
            throw new HubException($"Error increasing level of task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task DecreaseLevel(string projectId, int taskId)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.DecreaseTaskLevelAsync(id, taskId);

            await Clients.OthersInGroup(projectId).SendAsync("DecreasedLevel", new { id = taskId });
        }
        catch (Exception ex)
        {
            throw new HubException($"Error decreasing level of task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task CompleteTask(string projectId, int taskId)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.CompleteTaskAsync(id, taskId);

            await Clients.OthersInGroup(projectId).SendAsync("CompletedTask", new { id = taskId });
        }
        catch (Exception ex)
        {
            throw new HubException($"Error completing task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }

    public async Task UncompleteTask(string projectId, int taskId)
    {
        try
        {
            Guid id = new(projectId);

            await _tasksService.UncompleteTaskAsync(id, taskId);

            await Clients.OthersInGroup(projectId).SendAsync("UncompletedTask", new { id = taskId });
        }
        catch (Exception ex)
        {
            throw new HubException($"Error uncompleting task {taskId} of Project {projectId}: {ex.Message}");
        }
        finally
        {
            VerifyProjectAccessHub._semaphore.Release();
        }
    }
}
