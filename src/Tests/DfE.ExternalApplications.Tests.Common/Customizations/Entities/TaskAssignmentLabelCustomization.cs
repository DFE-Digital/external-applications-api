using AutoFixture;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class TaskAssignmentLabelCustomization : ICustomization
{
    public TaskAssignmentLabelId? OverrideId { get; set; }
    public string? OverrideValue { get; set; }
    public string? OverrideTaskId { get; set; }
    public UserId? OverrideUserId { get; set; }
    public DateTime? OverrideCreatedOn { get; set; }
    public UserId? OverrideCreatedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Domain.Entities.TaskAssignmentLabel>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new TaskAssignmentLabelId(fixture.Create<Guid>());
            var value = OverrideValue ?? fixture.Create<string>();
            var taskId = OverrideTaskId ?? fixture.Create<string>();
            var userId = OverrideUserId ?? new UserId(fixture.Create<Guid>());
            var createdOn = OverrideCreatedOn ?? fixture.Create<DateTime>();
            var createdBy = OverrideCreatedBy ?? new UserId(fixture.Create<Guid>());

            return new Domain.Entities.TaskAssignmentLabel(
                id,
                value,
                taskId,
                createdBy,
                createdOn,
                userId);
        }));
    }
} 