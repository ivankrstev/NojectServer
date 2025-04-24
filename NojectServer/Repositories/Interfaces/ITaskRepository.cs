using NojectServer.Repositories.Base;
using Task = NojectServer.Models.Task;

namespace NojectServer.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<Task>
{
    Task<IEnumerable<Task>> GetByProjectIdAsync(Guid projectId);
    Task<Task?> GetByProjectAndTaskIdAsync(Guid projectId, int taskId);
}
