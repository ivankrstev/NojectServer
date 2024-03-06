namespace NojectServer.Utils
{
    public static class TaskExtensions
    {
        public static void OrderTasks(this Models.Task[] tasks, int? first_task)
        {
            var taskMap = new Dictionary<int, int>(); // Maps task IDs to their index in the array
            for (int i = 0; i < tasks.Length; i++)
                taskMap[tasks[i].Id] = i;
            int currentIndex = 0;
            int? currentId = first_task;
            while (currentId.HasValue && taskMap.TryGetValue(currentId.Value, out var taskIndex))
            {
                // Swap the task at taskIndex with the task at currentIndex if they're not the same
                if (taskIndex != currentIndex)
                {
                    (tasks[taskIndex], tasks[currentIndex]) = (tasks[currentIndex], tasks[taskIndex]);
                    // Update the taskMap to reflect the swap
                    taskMap[tasks[taskIndex].Id] = taskIndex;
                    taskMap[tasks[currentIndex].Id] = currentIndex;
                }
                currentId = tasks[currentIndex].Next; // Move to the next task in the sequence
                currentIndex++; // Increment the position for the next task
            }
        }

        public static int GetTaskParentIndex(this Models.Task[] orderedTasks, int targetTaskIndex)
        {
            if (orderedTasks[targetTaskIndex].Level == 0)
                return -1;
            for (int i = targetTaskIndex - 1; i >= 0; i--)
                if (orderedTasks[i].Level < orderedTasks[targetTaskIndex].Level)
                    return i;
            return -1;
        }

        public static Models.Task[] GetTaskChildren(this Models.Task[] orderedTasks, int parentTaskIndex)
        {
            List<Models.Task> childTasks = new();
            int parentTaskLevel = orderedTasks[parentTaskIndex].Level;
            int currentTaskIndex = parentTaskIndex + 1;
            while (currentTaskIndex != orderedTasks.Length - 1 && parentTaskLevel != orderedTasks[currentTaskIndex].Level)
            {
                if (orderedTasks[parentTaskIndex].Level + 1 == orderedTasks[currentTaskIndex].Level)
                    childTasks.Add(orderedTasks[currentTaskIndex]);
                currentTaskIndex++;
            }
            return childTasks.ToArray();
        }

        public static Models.Task GetLastSubtaskOrDefaultTask(this Models.Task[] orderedTasks, int targetTaskIndex)
        {
            int targetTaskLevel = orderedTasks[targetTaskIndex].Level;
            Models.Task? foundTask = orderedTasks[targetTaskIndex];
            while (targetTaskIndex != orderedTasks.Length - 1 && targetTaskLevel < orderedTasks[++targetTaskIndex].Level)
                foundTask = orderedTasks[targetTaskIndex];
            return foundTask;
        }
    }
}