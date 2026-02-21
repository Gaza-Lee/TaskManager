namespace TaskManager.Models;

public enum TaskItemStatus
{
    Available,
    InProgress,
    Completed
}

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedBy { get; set; } = string.Empty;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Available;
    public bool NeedsHelp { get; set; }
    public bool NeedsModification { get; set; }
    public string HelpDetails { get; set; } = string.Empty;
    public string AuditRemark { get; set; } = string.Empty;
    public List<string> ModificationHistory { get; set; } = new();
    public List<TaskRemark> Remarks { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public string AcceptedBy { get; set; } = string.Empty;
}
