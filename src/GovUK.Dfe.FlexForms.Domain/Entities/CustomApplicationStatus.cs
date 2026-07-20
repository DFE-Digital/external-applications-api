using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System;

namespace GovUK.Dfe.FlexForms.Domain.Entities
{
    public sealed class CustomApplicationStatus : BaseAggregateRoot, IEntity<CustomApplicationStatusId>
    {
        public CustomApplicationStatusId? Id { get; private set; }
        public TemplateId TemplateId { get; private set; }
        public Template? Template { get; private set; }
        public ApplicationStatus ApplicationStatus { get; private set; }
        public string? Label { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public UserId CreatedBy { get; private set; }
        public User? CreatedByUser { get; private set; }

        public CustomApplicationStatus(
            CustomApplicationStatusId id,
            TemplateId templateId,
            ApplicationStatus applicationStatus,
            string? label,
            DateTime createdOn,
            UserId createdBy)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            TemplateId = templateId ?? throw new ArgumentNullException(nameof(templateId));
            ApplicationStatus = applicationStatus;
            Label = label;
            CreatedOn = createdOn;
            CreatedBy = createdBy ?? throw new ArgumentNullException(nameof(createdBy));
        }

        /// <summary>
        /// Updates the label for this custom application status.
        /// </summary>
        public void UpdateLabel(string? label)
        {
            Label = label;
        }
    }
}
