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
    }
}