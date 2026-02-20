using System;

namespace TaskManager.Models;

public enum NotificationType
{
    RemarkAdded,
    AuditHelp,
    TaskReassigned,
    StatusUpdate
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public NotificationType Type { get; set; }
}
