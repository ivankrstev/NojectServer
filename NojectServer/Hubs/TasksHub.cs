using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NojectServer.Data;
using NojectServer.Middlewares;
using NojectServer.Utils;

namespace NojectServer.Hubs
{
    [Authorize]
    public class TasksHub : Hub
    {
        private readonly DataContext _dataContext;

        public TasksHub(DataContext dataContext)
        {
            _dataContext = dataContext;
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
                var user = Context.User?.Identity?.Name!;
                Guid id = new(projectId);
                transaction = await _dataContext.Database.BeginTransactionAsync();
                var project = await _dataContext.Projects.FromSqlInterpolated($"SELECT * FROM projects WHERE project_id = {id} FOR UPDATE").FirstAsync();
                var tasks = await _dataContext.Tasks.Where(t => t.ProjectId == id).ToArrayAsync();
                tasks.OrderTasks(project.FirstTask);
                var maxId = tasks.Select(t => (int?)t.Id).Max() ?? 0; // Check the sorted tasks array for the maximum task id
                var prevTaskIndex = Array.FindIndex(tasks, t => t.Id == prevTaskId); // Find the index of the previous task in the sorted tasks array
                var targetForNewTask = prevTaskId != null ? tasks.GetLastSubtaskOrDefaultTask(prevTaskIndex) : null; // Find the last subtask of the previous task, if there is any
                Models.Task task = new()
                {
                    Id = maxId + 1,
                    ProjectId = id,
                    CreatedBy = user
                };
                await _dataContext.AddAsync(task);
                project.FirstTask ??= task.Id; // If no first task, set the new task as the first task
                if (targetForNewTask != null)
                {
                    // Change prev, next task pointers
                    task.Next = targetForNewTask.Next;
                    task.Level = tasks[prevTaskIndex].Level;
                    targetForNewTask.Next = task.Id;
                    // Set the parent task to be incomplete, due to adding a new incomplete subtask.
                    int parentTaskIndex = tasks.GetTaskParentIndex(prevTaskIndex);
                    while (parentTaskIndex != -1)
                    {
                        var parentTask = tasks[parentTaskIndex]; // Get the parent task
                        if (!parentTask.Completed)
                            break; // If the parent task is already uncompleted, stop
                        parentTask.Completed = false;
                        // Find and check the parent of the parent task, and so on
                        parentTaskIndex = tasks.GetTaskParentIndex(parentTaskIndex);
                    }
                }
                else
                {
                    var lastTask = await _dataContext.Tasks.Where(t => t.Id != task.Id && t.Next == null && t.ProjectId == id).FirstOrDefaultAsync();
                    if (lastTask != null)
                    {
                        lastTask.Next = task.Id;
                        task.Level = lastTask.Level;
                    }
                    // Adjust the completness of the parent task, if there is any. Or delete the
                    // adding task with null prevTask
                }
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await Clients.OthersInGroup(projectId).SendAsync("AddedTask", new { task });
                return new { task };
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw new HubException($"Error adding task to Project {projectId}");
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
                var user = Context.User?.Identity?.Name!;
                Guid id = new(projectId);
                var taskToUpdate = await _dataContext.Tasks
                    .Where(t => t.ProjectId == id && t.Id == taskId).FirstOrDefaultAsync() ?? throw new HubException($"Task ID {taskId} of project {projectId} not found.");
                taskToUpdate.Value = newValue;
                taskToUpdate.LastModifiedOn = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
                await Clients.OthersInGroup(projectId).SendAsync("ChangedValue", new { task = new { id = taskId, newValue } });
                return new { task = new { id = taskId, newValue } };
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new HubException($"Error changing value of Task {taskId} of Project {projectId}");
            }
            finally
            {
                VerifyProjectAccessHub._semaphore.Release();
            }
        }

        public async Task DeleteTask(string projectId, int taskId)
        {
            IDbContextTransaction? transaction = null;
            try
            {
                Guid id = new(projectId);
                transaction = await _dataContext.Database.BeginTransactionAsync();
                var project = await _dataContext.Projects.FromSqlInterpolated($"SELECT * FROM projects WHERE project_id = {id} FOR UPDATE").FirstAsync();
                var taskToDelete = await _dataContext.Tasks
                    .Where(t => t.ProjectId == id && t.Id == taskId).FirstOrDefaultAsync() ?? throw new HubException($"Task ID {taskId} of project {projectId} not found.");
                var tasks = await _dataContext.Tasks.Where(t => t.ProjectId == id).ToArrayAsync();
                tasks.OrderTasks(project.FirstTask);
                // Find the target task index in the sorted tasks array
                int targetTaskIndex = Array.FindIndex(tasks, item => item.Id == taskId);
                int targetTaskLevel = tasks[targetTaskIndex].Level;
                // Decrease the levels of all subtasks of the task to be deleted by 1, if there are any
                while (targetTaskIndex != tasks.Length - 1 && targetTaskLevel < tasks[++targetTaskIndex].Level)
                    tasks[targetTaskIndex].Level--;
                // Adjust the pointers
                if (taskId == project.FirstTask)
                    project.FirstTask = taskToDelete.Next; // Change the first task pointer, if the task to be deleted is the first task
                else
                {
                    var prevTask = await _dataContext.Tasks.Where(t => t.ProjectId == id && t.Next == taskId).FirstOrDefaultAsync();
                    if (prevTask != null)
                        prevTask.Next = taskToDelete.Next; // Change the next task pointer of the previous task
                }
                // Finally, delete the task
                _dataContext.Remove(taskToDelete);
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await Clients.OthersInGroup(projectId).SendAsync("DeletedTask", new { task = new { id = taskId } });
            }
            catch (HubException)
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw new HubException($"Error deleting Task {taskId} of Project {projectId}");
            }
            finally
            {
                VerifyProjectAccessHub._semaphore.Release();
            }
        }
    }
}