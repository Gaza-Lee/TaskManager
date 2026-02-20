using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

public class TaskService(TaskDbContext db, INotificationService notifications) : ITaskService
{
    public async Task<List<TaskItem>> GetTasksAsync()
    {
        return await db.Tasks
            .Include(t => t.Remarks)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> SearchTasksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetTasksAsync();

        var lower = query.ToLowerInvariant();
        return await db.Tasks
            .AsNoTracking()
            .Where(t => t.Title.ToLower().Contains(lower) || t.Description.ToLower().Contains(lower))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetTasksForUserAsync(string userName)
    {
        return await db.Tasks
            .AsNoTracking()
            .Where(t => t.AssignedTo.ToLower() == userName.ToLower())
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTime.UtcNow;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        var existing = await db.Tasks.FindAsync(task.Id);
        if (existing is null) return;

        existing.Title = task.Title;
        existing.Description = task.Description;
        existing.AssignedTo = task.AssignedTo;
        existing.AssignedBy = task.AssignedBy;
        existing.NeedsHelp = task.NeedsHelp;
        existing.HelpDetails = task.HelpDetails;
        existing.AuditRemark = task.AuditRemark;

        // Check for state changes to trigger notifications
        if (task.AssignedTo.ToLower() != existing.AssignedTo.ToLower() && !string.IsNullOrWhiteSpace(task.AssignedTo))
        {
            await notifications.AddNotificationAsync(new Notification {
                Recipient = task.AssignedTo,
                TaskId = task.Id,
                Message = $"Task '{task.Title}' has been reassigned to you.",
                Type = NotificationType.TaskReassigned
            });
        }

        if (task.Status == TaskItemStatus.Completed && existing.Status != TaskItemStatus.Completed)
        {
            existing.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(existing.AssignedBy) && existing.AssignedBy != task.AssignedTo)
            {
                await notifications.AddNotificationAsync(new Notification {
                    Recipient = existing.AssignedBy,
                    TaskId = task.Id,
                    Message = $"{task.AssignedTo} completed '{task.Title}'. Ready for audit.",
                    Type = NotificationType.StatusUpdate
                });
            }
        }
        else if (task.Status != TaskItemStatus.Completed)
        {
            existing.CompletedAt = null;
        }

        if (task.NeedsHelp && !existing.NeedsHelp)
        {
            if (!string.IsNullOrWhiteSpace(existing.AssignedBy) && existing.AssignedBy != task.AssignedTo)
            {
                await notifications.AddNotificationAsync(new Notification {
                    Recipient = existing.AssignedBy,
                    TaskId = task.Id,
                    Message = $"{task.AssignedTo} requested help with '{task.Title}'.",
                    Type = NotificationType.AuditHelp
                });
            }
        }

        existing.Status = task.Status;

        await db.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await db.Tasks.FindAsync(taskId);
        if (task is not null)
        {
            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddRemarkAsync(Guid taskId, TaskRemark remark)
    {
        remark.TaskId = taskId;
        remark.CreatedAt = DateTime.UtcNow;
        db.Remarks.Add(remark);
        
        // Notify stakeholders
        var task = await db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            // Notify the assigned user if someone else commented
            if (!string.IsNullOrWhiteSpace(task.AssignedTo) && task.AssignedTo != remark.Author)
            {
                await notifications.AddNotificationAsync(new Notification {
                    Recipient = task.AssignedTo,
                    TaskId = taskId,
                    Message = $"{remark.Author} added a remark to '{task.Title}'.",
                    Type = NotificationType.RemarkAdded
                });
            }
            // Notify the creator if someone else commented
            if (!string.IsNullOrWhiteSpace(task.AssignedBy) && task.AssignedBy != remark.Author && task.AssignedBy != task.AssignedTo)
            {
                await notifications.AddNotificationAsync(new Notification {
                    Recipient = task.AssignedBy,
                    TaskId = taskId,
                    Message = $"{remark.Author} added a remark to '{task.Title}'.",
                    Type = NotificationType.RemarkAdded
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
