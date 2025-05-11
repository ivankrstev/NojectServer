namespace NojectServer.Exceptions;

public class TaskLevelException : Exception
{
    public Guid ProjectId { get; }
    public int TaskId { get; }

    public TaskLevelException(Guid projectId, int taskId, string message)
        : base(message)
    {
        ProjectId = projectId;
        TaskId = taskId;
    }

    public TaskLevelException(Guid projectId, int taskId, string message, Exception innerException)
        : base(message, innerException)
    {
        ProjectId = projectId;
        TaskId = taskId;
    }
}
