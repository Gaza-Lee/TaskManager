using TaskManager.Models;

namespace TaskManager.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetTasksAsync();
    Task<List<TaskItem>> SearchTasksAsync(string query);
    Task<List<TaskItem>> GetTasksForUserAsync(string userName);
    Task AddTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(Guid taskId);
    Task AddRemarkAsync(Guid taskId, TaskRemark remark);
}
