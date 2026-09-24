namespace NojectServer.Exceptions;

public class TaskOperationException : Exception
{
    public Guid ProjectId { get; }
    public int? TaskId { get; }
    public string Operation { get; }

    public TaskOperationException(string operation, Guid projectId, int? taskId = null)
        : base($"Error {operation} task {(taskId.HasValue ? taskId.ToString() : string.Empty)} of Project {projectId}")
    {
        Operation = operation;
        ProjectId = projectId;
        TaskId = taskId;
    }

    public TaskOperationException(string operation, Guid projectId, int? taskId, Exception innerException)
        : base($"Error {operation} task {(taskId.HasValue ? taskId.ToString() : string.Empty)} of Project {projectId}", innerException)
    {
        Operation = operation;
        ProjectId = projectId;
        TaskId = taskId;
    }
}
