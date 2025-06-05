using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class TaskAssignmentLabel : BaseAggregateRoot, IEntity<TaskAssignmentLabelId>
{
    public TaskAssignmentLabelId? Id { get; private set; }
    public string Value { get; private set; } = null!;
    public string TaskId { get; private set; }
    public UserId? UserId { get; private set; }
    public User? AssignedUser { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public UserId CreatedBy { get; private set; }
    public User? CreatedByUser { get; private set; }

    private TaskAssignmentLabel() { /* For EF Core */ }

    public TaskAssignmentLabel(
        TaskAssignmentLabelId id,
        string value,
        string taskId,
        UserId createdBy,
        DateTime? createdOn = null,
        UserId? userId = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        TaskId = taskId ?? throw new ArgumentNullException(nameof(taskId));
        CreatedBy = createdBy;
        CreatedOn = createdOn ?? DateTime.UtcNow;
        UserId = userId;
    }
}