namespace NojectServer.Exceptions;

public class TaskNotFoundException : Exception
{
    public Guid ProjectId { get; }
    public int TaskId { get; }

    public TaskNotFoundException(Guid projectId, int taskId)
        : base($"Task ID {taskId} of project {projectId} not found.")
    {
        ProjectId = projectId;
        TaskId = taskId;
    }

    public TaskNotFoundException(Guid projectId, int taskId, Exception innerException)
        : base($"Task ID {taskId} of project {projectId} not found.", innerException)
    {
        ProjectId = projectId;
        TaskId = taskId;
    }
}
