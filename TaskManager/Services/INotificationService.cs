using TaskManager.Models;

namespace TaskManager.Services;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsForUserAsync(string userName);
    Task AddNotificationAsync(Notification notification);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(string userName);
    Task<int> GetUnreadCountAsync(string userName);
    event Action? OnNotificationsChanged;
}
