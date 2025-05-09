namespace NojectServer.Services.Tasks.Interfaces;

public interface ITasksService
{
    Task<Models.Task> AddTaskAsync(Guid projectId, Guid userId, int? prevTaskId = null);
    Task<Models.Task> ChangeValueAsync(Guid projectId, int taskId, string newValue);
    Task DeleteTaskAsync(Guid projectId, int taskId);
    Task IncreaseTaskLevelAsync(Guid projectId, int taskId);
    Task DecreaseTaskLevelAsync(Guid projectId, int taskId);
    Task CompleteTaskAsync(Guid projectId, int taskId);
    Task UncompleteTaskAsync(Guid projectId, int taskId);
}
