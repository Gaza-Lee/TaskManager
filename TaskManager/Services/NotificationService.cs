using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

public class NotificationService(TaskDbContext db) : INotificationService
{
    public event Action? OnNotificationsChanged;

    public async Task<List<Notification>> GetNotificationsForUserAsync(string userName)
    {
        return await db.Notifications
            .AsNoTracking()
            .Where(n => n.Recipient.ToLower() == userName.ToLower())
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task AddNotificationAsync(Notification notification)
    {
        notification.Id = Guid.NewGuid();
        notification.CreatedAt = DateTime.UtcNow;
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
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
        var unread = await db.Notifications
            .Where(n => n.Recipient.ToLower() == userName.ToLower() && !n.IsRead)
            .ToListAsync();
        
        foreach (var n in unread) n.IsRead = true;
        await db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task ClearAllNotificationsAsync(string userName)
    {
        var notifications = await db.Notifications
            .Where(n => n.Recipient.ToLower() == userName.ToLower())
            .ToListAsync();
        
        db.Notifications.RemoveRange(notifications);
        await db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task<int> GetUnreadCountAsync(string userName)
    {
        return await db.Notifications
            .CountAsync(n => n.Recipient.ToLower() == userName.ToLower() && !n.IsRead);
    }
}
