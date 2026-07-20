using AutoFixture;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

public class ApplicationCustomization : ICustomization
{
    public ApplicationId? OverrideId { get; set; }
    public string? OverrideReference { get; set; }
    public TemplateVersionId? OverrideTemplateVersionId { get; set; }
    public DateTime? OverrideCreatedOn { get; set; }
    public UserId? OverrideCreatedBy { get; set; }
    public ApplicationStatus? OverrideStatus { get; set; }
    public DateTime? OverrideLastModifiedOn { get; set; }
    public UserId? OverrideLastModifiedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Domain.Entities.Application>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new ApplicationId(fixture.Create<Guid>());
            var reference = (OverrideReference ?? fixture.Create<string>()).Trim();
            var templateVersionId = OverrideTemplateVersionId ?? new TemplateVersionId(fixture.Create<Guid>());
            var createdOn = OverrideCreatedOn ?? fixture.Create<DateTime>();
            var createdBy = OverrideCreatedBy ?? new UserId(fixture.Create<Guid>());
            var status = OverrideStatus;
            var lastModifiedOn = OverrideLastModifiedOn;
            var lastModifiedBy = OverrideLastModifiedBy;

            return new Domain.Entities.Application(
                id,
                reference,
                templateVersionId,
                createdOn,
                createdBy,
                status,
                lastModifiedOn,
                lastModifiedBy);
        }));
    }
}