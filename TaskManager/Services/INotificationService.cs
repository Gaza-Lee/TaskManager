using TaskManager.Models;

namespace TaskManager.Services;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsForUserAsync(string userName);
    Task EnqueueNotificationAsync(Notification notification);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(string userName);
    Task ClearAllNotificationsAsync(string userName);
    Task<int> GetUnreadCountAsync(string userName);
    void RaiseNotificationsChanged();
    event Action? OnNotificationsChanged;
}
