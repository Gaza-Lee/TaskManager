using System;

namespace TaskManager.Services
{
    /// <summary>
    /// Manages client-side UI state for tasks, such as which task is currently being discussed.
    /// </summary>
    public class TaskStateService
    {
        public Guid? ActiveTaskId { get; private set; }

        public event Action? OnChange;

        public void SetActiveTask(Guid? taskId)
        {
            if (ActiveTaskId != taskId)
            {
                ActiveTaskId = taskId;
                NotifyStateChanged();
            }
        }

        public void ClearActiveTask()
        {
            if (ActiveTaskId != null)
            {
                ActiveTaskId = null;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
