using AutoFixture;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class TemplateVersionCustomization : ICustomization
{
    public TemplateVersionId? OverrideId { get; set; }
    public TemplateId? OverrideTemplateId { get; set; }
    public string? OverrideVersionNumber { get; set; }
    public string? OverrideJsonSchema { get; set; }
    public DateTime? OverrideCreatedOn { get; set; }
    public UserId? OverrideCreatedBy { get; set; }
    public DateTime? OverrideLastModifiedOn { get; set; }
    public UserId? OverrideLastModifiedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<TemplateVersion>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new TemplateVersionId(fixture.Create<Guid>());
            var templateId = OverrideTemplateId ?? new TemplateId(fixture.Create<Guid>());
            var versionNumber = (OverrideVersionNumber ?? fixture.Create<string>()).Trim();
            var jsonSchema = (OverrideJsonSchema ?? "{\"type\":\"object\"}").Trim();
            var createdOn = OverrideCreatedOn ?? fixture.Create<DateTime>();
            var createdBy = OverrideCreatedBy ?? new UserId(fixture.Create<Guid>());
            var lastModifiedOn = OverrideLastModifiedOn;
            var lastModifiedBy = OverrideLastModifiedBy;

            return new TemplateVersion(
                id,
                templateId,
                versionNumber,
                jsonSchema,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy);
        }));
    }
}