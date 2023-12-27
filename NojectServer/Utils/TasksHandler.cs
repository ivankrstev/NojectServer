namespace NojectServer.Utils
{
    public class TasksHandler
    {
        public static List<Models.Task> OrderTasks(Models.Task[] unorderedTasks, int? first_task)
        {
            if (unorderedTasks.Length == 0 || first_task == null) return new List<Models.Task>();
            if (unorderedTasks.Length == 1) return new List<Models.Task>() { unorderedTasks[0] };
            int? current_next = null;
            List<Models.Task> orderedTasks = new();
            for (int i = 0; i < unorderedTasks.Length; i++)
            {
                int id = unorderedTasks[i].Id;
                int? next = unorderedTasks[i].Next;
                if (orderedTasks.Count == 0 && id == first_task)
                {
                    orderedTasks.Add(unorderedTasks[i]);
                    unorderedTasks = unorderedTasks.Where(x => x.Id != id).ToArray();
                    current_next = next;
                    i = -1;
                }
                else if (current_next == id)
                {
                    orderedTasks.Add(unorderedTasks[i]);
                    unorderedTasks = unorderedTasks.Where(x => x.Id != id).ToArray();
                    current_next = next; // Set the pointer for the next element to be searched and added to the array
                    i = -1;
                    if (next == null) break;
                }
            }
            return orderedTasks;
        }
    }
}