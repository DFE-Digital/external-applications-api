using AutoFixture;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

public class TemplateCustomization : ICustomization
{
    public TemplateId? OverrideId { get; set; }
    public string? OverrideName { get; set; }
    public DateTime? OverrideCreatedOn { get; set; }
    public UserId? OverrideCreatedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Template>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new TemplateId(fixture.Create<Guid>());
            var name = (OverrideName ?? fixture.Create<string>()).Trim();
            var createdOn = OverrideCreatedOn ?? fixture.Create<DateTime>();
            var createdBy = OverrideCreatedBy ?? new UserId(fixture.Create<Guid>());

            return new Template(
                id,
                name,
                createdOn,
                createdBy);
        }));
    }
}