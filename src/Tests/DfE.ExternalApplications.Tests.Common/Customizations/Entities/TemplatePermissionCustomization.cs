using AutoFixture;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities
{
    public class TemplatePermissionCustomization : ICustomization
    {
        public TemplatePermissionId? OverrideId { get; set; }
        public UserId? OverrideUserId { get; set; }
        public TemplateId? OverrideTemplateId { get; set; }
        public AccessType? OverridePermissionType { get; set; }
        public DateTime? OverrideGrantedOn { get; set; }
        public UserId? OverrideGrantedBy { get; set; }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<TemplatePermission>(composer => composer.FromFactory(() =>
            {
                var id = OverrideId ?? new TemplatePermissionId(fixture.Create<Guid>());
                var userId = OverrideUserId ?? new UserId(fixture.Create<Guid>());
                var templateId = OverrideTemplateId ?? new TemplateId(fixture.Create<Guid>());
                var permissionType = OverridePermissionType ?? fixture.Create<AccessType>();
                var grantedOn = OverrideGrantedOn ?? fixture.Create<DateTime>();
                var grantedBy = OverrideGrantedBy ?? new UserId(fixture.Create<Guid>());

                return new TemplatePermission(
                    id,
                    userId,
                    templateId,
                    permissionType,
                    grantedOn,
                    grantedBy);
            }));
        }
    }
}
