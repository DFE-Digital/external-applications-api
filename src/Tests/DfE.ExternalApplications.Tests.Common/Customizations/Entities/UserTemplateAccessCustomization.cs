using AutoFixture;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class UserTemplateAccessCustomization : ICustomization
{
    public UserTemplateAccessId? OverrideId { get; set; }
    public UserId? OverrideUserId { get; set; }
    public TemplateId? OverrideTemplateId { get; set; }
    public DateTime? OverrideGrantedOn { get; set; }
    public UserId? OverrideGrantedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<UserTemplateAccess>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new UserTemplateAccessId(fixture.Create<Guid>());
            var userId = OverrideUserId ?? new UserId(fixture.Create<Guid>());
            var templateId = OverrideTemplateId ?? new TemplateId(fixture.Create<Guid>());
            var grantedOn = OverrideGrantedOn ?? fixture.Create<DateTime>();
            var grantedBy = OverrideGrantedBy ?? new UserId(fixture.Create<Guid>());

            return new UserTemplateAccess(
                id,
                userId,
                templateId,
                grantedOn,
                grantedBy);
        }));
    }
}