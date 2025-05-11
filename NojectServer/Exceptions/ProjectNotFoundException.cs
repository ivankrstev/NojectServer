namespace NojectServer.Exceptions;

public class ProjectNotFoundException : Exception
{
    public Guid ProjectId { get; }

    public ProjectNotFoundException(Guid projectId)
        : base($"Project {projectId} not found.")
    {
        ProjectId = projectId;
    }

    public ProjectNotFoundException(Guid projectId, Exception innerException)
        : base($"Project {projectId} not found.", innerException)
    {
        ProjectId = projectId;
    }
}
