using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfE.ExternalApplications.Domain.Entities
{
    public sealed class TemplatePermission : BaseAggregateRoot, IEntity<TemplatePermissionId>
    {
        public TemplatePermissionId? Id { get; private set; }
        public UserId UserId { get; private set; }
        public User? User { get; private set; }
        public TemplateId TemplateId { get; private set; }
        public Template? Template { get; private set; }
        public AccessType AccessType { get; private set; }
        public DateTime GrantedOn { get; private set; }
        public UserId GrantedBy { get; private set; }
        public User? GrantedByUser { get; private set; }

        private TemplatePermission() { /* For EF Core */ }

        public TemplatePermission(
            TemplatePermissionId id,
            UserId userId,
            TemplateId templateId,
            AccessType accessType,
            DateTime grantedOn,
            UserId grantedBy)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            TemplateId = templateId ?? throw new ArgumentNullException(nameof(templateId));
            AccessType = accessType;
            GrantedOn = grantedOn;
            GrantedBy = grantedBy;
        }
    }
}
