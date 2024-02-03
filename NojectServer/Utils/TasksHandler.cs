namespace NojectServer.Utils
{
    public class TasksHandler
    {
        public static List<Models.Task> OrderTasks(Models.Task[] unorderedTasks, int? first_task)
        {
            if (unorderedTasks.Length == 0 || first_task == null) return new List<Models.Task>();
            if (unorderedTasks.Length == 1) return new List<Models.Task>() { unorderedTasks[0] };
            var taskMap = new Dictionary<int, Models.Task>();
            foreach (var task in unorderedTasks)
                taskMap[task.Id] = task;
            List<Models.Task> orderedTasks = new();
            int? currentId = first_task;
            while (currentId != null)
            {
                if (taskMap.TryGetValue(currentId.Value, out var currentTask))
                {
                    orderedTasks.Add(currentTask);
                    currentId = currentTask.Next;
                }
                else
                    break;
            }
            return orderedTasks;
        }
    }
}