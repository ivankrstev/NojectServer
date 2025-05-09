using NojectServer.Repositories.Base;
using Task = NojectServer.Models.Task;

namespace NojectServer.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<Task, (int Id, Guid ProjectId)>
{
    Task<IEnumerable<Task>> GetByProjectIdAsync(Guid projectId);
    Task<Task?> GetByProjectAndTaskIdAsync(Guid projectId, int taskId);
}
