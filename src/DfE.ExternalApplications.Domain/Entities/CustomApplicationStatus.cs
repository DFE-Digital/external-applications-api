using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using System;

namespace DfE.ExternalApplications.Domain.Entities
{
    public sealed class CustomApplicationStatus : BaseAggregateRoot, IEntity<CustomApplicationStatusId>
    {
        public CustomApplicationStatusId? Id { get; private set; }
        public TemplateId TemplateId { get; private set; }
        public Template? Template { get; private set; }
        public int ApplicationStatus { get; private set; }
        public string Label { get; private set; } = null!;
        public DateTime CreatedOn { get; private set; }
        public UserId CreatedBy { get; private set; }
        public User? CreatedByUser { get; private set; }

        private CustomApplicationStatus() { /* For EF Core */ }

        public CustomApplicationStatus(
            CustomApplicationStatusId id,
            TemplateId templateId,
            int applicationStatus,
            string label,
            DateTime createdOn,
            UserId createdBy)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            TemplateId = templateId ?? throw new ArgumentNullException(nameof(templateId));
            ApplicationStatus = applicationStatus;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            CreatedOn = createdOn;
            CreatedBy = createdBy;
        }
    }
}
