using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Tasks.Interfaces;
using NojectServer.Utils;

namespace NojectServer.Services.Tasks.Implementations;

public class TasksService(IUnitOfWork unitOfWork) : ITasksService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Models.Task> AddTaskAsync(Guid projectId, Guid userId, int? prevTaskId = null)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            var maxId = tasks.Select(t => (int?)t.Id).Max() ?? 0;
            var prevTaskIndex = Array.FindIndex(tasks, t => t.Id == prevTaskId);
            var targetForNewTask = prevTaskId != null ? tasks.GetLastSubtaskOrDefaultTask(prevTaskIndex) : null;

            Models.Task task = new()
            {
                Id = maxId + 1,
                ProjectId = projectId,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };

            project.FirstTask ??= task.Id;

            if (targetForNewTask != null)
            {
                task.Next = targetForNewTask.Next;
                task.Level = tasks[prevTaskIndex].Level;
                targetForNewTask.Next = task.Id;

                // Set parent tasks to incomplete if needed
                int parentTaskIndex = tasks.GetTaskParentIndex(prevTaskIndex);
                while (parentTaskIndex != -1)
                {
                    var parentTask = tasks[parentTaskIndex];
                    if (!parentTask.Completed)
                        break;
                    parentTask.Completed = false;
                    _unitOfWork.Tasks.Update(parentTask);
                    parentTaskIndex = tasks.GetTaskParentIndex(parentTaskIndex);
                }
            }
            else
            {
                var lastTask = tasks.FirstOrDefault(t => t.Id != task.Id && t.Next == null);
                if (lastTask != null)
                {
                    lastTask.Next = task.Id;
                    task.Level = lastTask.Level;
                    _unitOfWork.Tasks.Update(lastTask);
                }
            }

            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return task;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error adding task to Project {projectId}", ex);
        }
    }

    public async Task<Models.Task> ChangeValueAsync(Guid projectId, int taskId, string newValue)
    {
        try
        {
            var taskToUpdate = await _unitOfWork.Tasks.GetByProjectAndTaskIdAsync(projectId, taskId)
                ?? throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            taskToUpdate.Value = newValue;
            taskToUpdate.LastModifiedOn = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(taskToUpdate);
            await _unitOfWork.SaveChangesAsync();

            return taskToUpdate;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error changing value of Task {taskId} of Project {projectId}", ex);
        }
    }

    public async Task DeleteTaskAsync(Guid projectId, int taskId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var taskToDelete = await _unitOfWork.Tasks.GetByProjectAndTaskIdAsync(projectId, taskId)
                ?? throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            // Find the target task index in the sorted tasks array
            int targetTaskIndex = Array.FindIndex(tasks, item => item.Id == taskId);
            int targetTaskLevel = tasks[targetTaskIndex].Level;

            // Decrease the levels of all subtasks of the task to be deleted
            while (targetTaskIndex != tasks.Length - 1 && targetTaskLevel < tasks[++targetTaskIndex].Level)
            {
                tasks[targetTaskIndex].Level--;
                _unitOfWork.Tasks.Update(tasks[targetTaskIndex]);
            }

            // Adjust the pointers
            if (taskId == project.FirstTask)
            {
                project.FirstTask = taskToDelete.Next;
                _unitOfWork.Projects.Update(project);
            }
            else
            {
                var prevTask = tasks.FirstOrDefault(t => t.Next == taskId);
                if (prevTask != null)
                {
                    prevTask.Next = taskToDelete.Next;
                    _unitOfWork.Tasks.Update(prevTask);
                }
            }

            // Delete the task
            _unitOfWork.Tasks.Remove(taskToDelete);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error deleting Task {taskId} of Project {projectId}", ex);
        }
    }

    public async Task IncreaseTaskLevelAsync(Guid projectId, int taskId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            // Find the target task
            var targetTaskIndex = Array.FindIndex(tasks, task => task.Id == taskId);
            if (targetTaskIndex == -1)
                throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            var targetTask = tasks[targetTaskIndex];
            var prevTask = tasks.FirstOrDefault(t => t.Next == taskId);

            if (prevTask == null || prevTask.Level < targetTask.Level)
                throw new Exception($"Maximum level reached for Task {taskId} of Project {projectId}");

            targetTask.Level++; // Increase the level

            // Find the new parent task index and update completion status
            var newParentTaskIndex = tasks.GetTaskParentIndex(targetTaskIndex);
            if (newParentTaskIndex != -1)
            {
                if (!targetTask.Completed && tasks[newParentTaskIndex].Completed)
                {
                    tasks[newParentTaskIndex].Completed = false;
                    _unitOfWork.Tasks.Update(tasks[newParentTaskIndex]);
                }
                else if (targetTask.Completed && !tasks[newParentTaskIndex].Completed)
                {
                    int parentTaskIndex = newParentTaskIndex;
                    while (parentTaskIndex != -1)
                    {
                        var parentTask = tasks[parentTaskIndex];
                        var childrenOfParentTask = tasks.GetTaskChildren(parentTaskIndex);
                        if (childrenOfParentTask.All(task => task.Completed))
                        {
                            parentTask.Completed = true;
                            _unitOfWork.Tasks.Update(parentTask);
                        }
                        else break;
                        parentTaskIndex = tasks.GetTaskParentIndex(parentTaskIndex);
                    }
                }
            }

            _unitOfWork.Tasks.Update(targetTask);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error increasing level of task {taskId} of Project {projectId}", ex);
        }
    }

    public async Task DecreaseTaskLevelAsync(Guid projectId, int taskId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            // Find the target task
            int targetTaskIndex = Array.FindIndex(tasks, item => item.Id == taskId);
            if (targetTaskIndex == -1)
                throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            int targetTaskLevel = tasks[targetTaskIndex].Level;
            if (targetTaskLevel == 0)
                throw new Exception($"Minimum level reached for Task {taskId} of Project {projectId}");

            int oldParentTaskIndex = tasks.GetTaskParentIndex(targetTaskIndex);

            int nextTaskIndex = targetTaskIndex;
            // Decrease the levels of all subtasks
            while (nextTaskIndex != tasks.Length - 1 && targetTaskLevel < tasks[++nextTaskIndex].Level)
            {
                tasks[nextTaskIndex].Level--;
                _unitOfWork.Tasks.Update(tasks[nextTaskIndex]);
            }

            // Decrease level of the target task
            tasks[targetTaskIndex].Level--;
            _unitOfWork.Tasks.Update(tasks[targetTaskIndex]);

            // Update completion status of old parent if needed
            if (oldParentTaskIndex != -1)
            {
                var childrenOfParentTask = tasks.GetTaskChildren(oldParentTaskIndex);
                if (childrenOfParentTask.Length != 0 && childrenOfParentTask.All(task => task.Completed))
                {
                    tasks[oldParentTaskIndex].Completed = true;
                    _unitOfWork.Tasks.Update(tasks[oldParentTaskIndex]);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error decreasing level of task {taskId} of Project {projectId}", ex);
        }
    }

    public async Task CompleteTaskAsync(Guid projectId, int taskId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            // Find the target task
            var targetTaskIndex = Array.FindIndex(tasks, task => task.Id == taskId);
            if (targetTaskIndex == -1)
                throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            var targetTask = tasks[targetTaskIndex];
            int targetTaskLevel = targetTask.Level;

            // Complete all subtasks
            int nextTaskIndex = targetTaskIndex;
            while (nextTaskIndex != tasks.Length - 1 && targetTaskLevel < tasks[++nextTaskIndex].Level)
            {
                tasks[nextTaskIndex].Completed = true;
                _unitOfWork.Tasks.Update(tasks[nextTaskIndex]);
            }

            targetTask.Completed = true;
            _unitOfWork.Tasks.Update(targetTask);

            // Update parent tasks recursively
            int parentTaskIndex = tasks.GetTaskParentIndex(targetTaskIndex);
            while (parentTaskIndex != -1)
            {
                var parentTask = tasks[parentTaskIndex];
                var childrenOfParentTask = tasks.GetTaskChildren(parentTaskIndex);
                if (childrenOfParentTask.All(task => task.Completed))
                {
                    parentTask.Completed = true;
                    _unitOfWork.Tasks.Update(parentTask);
                }
                else break;
                parentTaskIndex = tasks.GetTaskParentIndex(parentTaskIndex);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error completing task {taskId} of Project {projectId}", ex);
        }
    }

    public async Task UncompleteTaskAsync(Guid projectId, int taskId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new Exception($"Project {projectId} not found.");

            var tasks = (await _unitOfWork.Tasks.GetByProjectIdAsync(projectId)).ToArray();
            tasks.OrderTasks(project.FirstTask);

            // Find the target task
            var targetTaskIndex = Array.FindIndex(tasks, task => task.Id == taskId);
            if (targetTaskIndex == -1)
                throw new Exception($"Task ID {taskId} of project {projectId} not found.");

            var targetTask = tasks[targetTaskIndex];
            int targetTaskLevel = targetTask.Level;

            // Uncomplete all subtasks
            int nextTaskIndex = targetTaskIndex;
            while (nextTaskIndex != tasks.Length - 1 && targetTaskLevel < tasks[++nextTaskIndex].Level)
            {
                tasks[nextTaskIndex].Completed = false;
                _unitOfWork.Tasks.Update(tasks[nextTaskIndex]);
            }

            targetTask.Completed = false;
            _unitOfWork.Tasks.Update(targetTask);

            // Update parent tasks recursively
            int parentTaskIndex = tasks.GetTaskParentIndex(targetTaskIndex);
            while (parentTaskIndex != -1)
            {
                var parentTask = tasks[parentTaskIndex];
                if (!parentTask.Completed)
                    break;

                parentTask.Completed = false;
                _unitOfWork.Tasks.Update(parentTask);
                parentTaskIndex = tasks.GetTaskParentIndex(parentTaskIndex);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Error uncompleting task {taskId} of Project {projectId}", ex);
        }
    }
}
