using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

public class NotificationService(IDbContextFactory<TaskDbContext> dbFactory) : INotificationService
{
    public event Action? OnNotificationsChanged;
    public void RaiseNotificationsChanged() => OnNotificationsChanged?.Invoke();

    public async Task<List<Notification>> GetNotificationsForUserAsync(string userName)
    {
        using var db = dbFactory.CreateDbContext();
        return await db.Notifications
            .AsNoTracking()
            .Where(n => n.Recipient.ToLower() == userName.ToLower())
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task EnqueueNotificationAsync(Notification notification)
    {
        using var db = dbFactory.CreateDbContext();
        notification.Id = Guid.NewGuid();
        notification.CreatedAt = DateTime.UtcNow;
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        using var db = dbFactory.CreateDbContext();
        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
            OnNotificationsChanged?.Invoke();
        }
    }

    public async Task MarkAllAsReadAsync(string userName)
    {
        using var db = dbFactory.CreateDbContext();
        var unread = await db.Notifications
            .Where(n => n.Recipient.ToLower() == userName.ToLower() && !n.IsRead)
            .ToListAsync();
        
        foreach (var n in unread) n.IsRead = true;
        await db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task ClearAllNotificationsAsync(string userName)
    {
        using var db = dbFactory.CreateDbContext();
        var notifications = await db.Notifications
            .Where(n => n.Recipient.ToLower() == userName.ToLower())
            .ToListAsync();
        
        db.Notifications.RemoveRange(notifications);
        await db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task<int> GetUnreadCountAsync(string userName)
    {
        using var db = dbFactory.CreateDbContext();
        return await db.Notifications
            .CountAsync(n => n.Recipient.ToLower() == userName.ToLower() && !n.IsRead);
    }
}
