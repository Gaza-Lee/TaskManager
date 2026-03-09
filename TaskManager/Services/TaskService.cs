using System;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

public class TaskService(IDbContextFactory<TaskDbContext> dbFactory, INotificationService notifications) : ITaskService
{
    public event Action? OnTasksChanged;

    public async Task<List<TaskItem>> GetTasksAsync()
    {
        using var db = dbFactory.CreateDbContext();
        return await db.Tasks
            .Include(t => t.Remarks)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskAsync(Guid id)
    {
        using var db = dbFactory.CreateDbContext();
        return await db.Tasks
            .Include(t => t.Remarks)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<TaskItem>> SearchTasksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetTasksAsync();

        using var db = dbFactory.CreateDbContext();
        var lower = query.ToLowerInvariant();
        return await db.Tasks
            .AsNoTracking()
            .Where(t => t.Title.ToLower().Contains(lower) || t.Description.ToLower().Contains(lower))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetTasksForUserAsync(string userName)
    {
        using var db = dbFactory.CreateDbContext();
        return await db.Tasks
            .AsNoTracking()
            .Where(t => t.AssignedTo.ToLower() == userName.ToLower())
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        using var db = dbFactory.CreateDbContext();
        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTime.UtcNow;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        OnTasksChanged?.Invoke();
        notifications.RaiseNotificationsChanged();
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        using var db = dbFactory.CreateDbContext();
        var existing = await db.Tasks.FindAsync(task.Id);
        if (existing is null) return;

        // Capture original values BEFORE mutations so change-detection comparisons are valid
        var originalAssignedTo = existing.AssignedTo;
        var wasCompleted = existing.Status == TaskItemStatus.Completed;
        var wasNeedsHelp = existing.NeedsHelp;
        var wasNeedsModification = existing.NeedsModification;

        existing.Title = task.Title;
        existing.Description = task.Description;
        existing.AssignedTo = task.AssignedTo;
        existing.AssignedBy = task.AssignedBy;
        existing.NeedsHelp = task.NeedsHelp;
        existing.NeedsModification = task.NeedsModification;
        existing.HelpDetails = task.HelpDetails;
        existing.AuditRemark = task.AuditRemark;
        existing.ModificationHistory = task.ModificationHistory;
        existing.AcceptedBy = task.AcceptedBy;
        existing.CompletedBy = task.CompletedBy;

        // Check for state changes to trigger notifications (use original captured values)
        if (!task.AssignedTo.Equals(originalAssignedTo, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(task.AssignedTo))
        {
            await notifications.EnqueueNotificationAsync(new Notification {
                Recipient = task.AssignedTo,
                TaskId = task.Id,
                Message = $"Task '{task.Title}' has been reassigned to you.",
                Type = NotificationType.TaskReassigned
            });
        }

        if (task.Status == TaskItemStatus.InProgress && existing.Status == TaskItemStatus.Available)
        {
            if (!string.IsNullOrWhiteSpace(existing.AssignedBy) && !existing.AssignedBy.Equals(task.AcceptedBy, StringComparison.OrdinalIgnoreCase))
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = existing.AssignedBy,
                    TaskId = task.Id,
                    Message = $"{task.AcceptedBy} accepted your task '{task.Title}'.",
                    Type = NotificationType.TaskAccepted
                });
            }
        }

        if (task.Status == TaskItemStatus.Completed && existing.Status != TaskItemStatus.Completed)
        {
            existing.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(existing.AssignedBy) && existing.AssignedBy != (task.CompletedBy ?? task.AcceptedBy))
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = existing.AssignedBy,
                    TaskId = task.Id,
                    Message = $"{task.CompletedBy ?? task.AcceptedBy} completed '{task.Title}'. Ready for audit.",
                    Type = NotificationType.StatusUpdate
                });
            }
        }
        else if (task.Status != TaskItemStatus.Completed)
        {
            existing.CompletedAt = null;
        }

        if (task.NeedsHelp && !wasNeedsHelp)
        {
            if (!string.IsNullOrWhiteSpace(existing.AssignedBy) && existing.AssignedBy != task.AssignedTo)
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = existing.AssignedBy,
                    TaskId = task.Id,
                    Message = $"{task.AssignedTo} requested help with '{task.Title}'.",
                    Type = NotificationType.AuditHelp
                });
            }
        }

        if (task.NeedsModification && !wasNeedsModification)
        {
            if (!string.IsNullOrWhiteSpace(existing.AssignedTo))
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = existing.AssignedTo,
                    TaskId = task.Id,
                    Message = $"Modifications requested for '{task.Title}'.",
                    Type = NotificationType.NeedsModification
                });
            }
        }

        existing.Status = task.Status;

        await db.SaveChangesAsync();
        OnTasksChanged?.Invoke();
        notifications.RaiseNotificationsChanged();
    }

    public async Task DeleteTaskAsync(Guid taskId, string userName)
    {
        using var db = dbFactory.CreateDbContext();
        var task = await db.Tasks.FindAsync(taskId);
        if (task is not null)
        {
            // Only the creator can delete the task
            if (task.AssignedBy.Equals(userName, StringComparison.OrdinalIgnoreCase))
            {
                db.Tasks.Remove(task);
                await db.SaveChangesAsync();
                OnTasksChanged?.Invoke();
            }
        }
    }

    public async Task AddRemarkAsync(Guid taskId, TaskRemark remark)
    {
        using var db = dbFactory.CreateDbContext();
        remark.TaskId = taskId;
        remark.CreatedAt = DateTime.UtcNow;
        db.Remarks.Add(remark);
        
        // Notify stakeholders
        var task = await db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            // Notify the "worker" (AcceptedBy or AssignedTo) if someone else commented
            var worker = !string.IsNullOrWhiteSpace(task.AcceptedBy) ? task.AcceptedBy : task.AssignedTo;
            if (!string.IsNullOrWhiteSpace(worker) && worker != remark.Author)
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = worker,
                    TaskId = taskId,
                    Message = $"{remark.Author} added a remark to '{task.Title}'.",
                    Type = NotificationType.RemarkAdded
                });
            }

            // Notify the creator if someone else commented
            if (!string.IsNullOrWhiteSpace(task.AssignedBy) && task.AssignedBy != remark.Author && task.AssignedBy != worker)
            {
                await notifications.EnqueueNotificationAsync(new Notification {
                    Recipient = task.AssignedBy,
                    TaskId = taskId,
                    Message = $"{remark.Author} added a remark to '{task.Title}'.",
                    Type = NotificationType.RemarkAdded
                });
            }
        }

        await db.SaveChangesAsync();
        OnTasksChanged?.Invoke();
        notifications.RaiseNotificationsChanged();
    }
}
