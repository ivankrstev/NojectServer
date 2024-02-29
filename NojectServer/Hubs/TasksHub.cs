﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NojectServer.Data;
using NojectServer.Middlewares;

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
                if (prevTask != null)
                {
                    prevTask.Next = task.Id;
                    task.Level = prevTask.Level;
                }
                else
                {
                    var lastTask = await _dataContext.Tasks.Where(t => t.Id != task.Id && t.Next == null && t.ProjectId == id).FirstOrDefaultAsync();
                    if (lastTask != null)
                    {
                        lastTask.Next = task.Id;
                        task.Level = lastTask.Level;
                    }
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
                var unorderedTasks = await _dataContext.Tasks.Where(t => t.ProjectId == id).ToArrayAsync();
                Models.Task[] tasks = TasksHandler.OrderTasks(unorderedTasks, project.FirstTask).ToArray();
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