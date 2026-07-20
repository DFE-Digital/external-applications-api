using AutoFixture;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

public class RoleCustomization : ICustomization
{
    public RoleId? OverrideId { get; set; }
    public string? OverrideName { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Role>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new RoleId(fixture.Create<Guid>());
            var name = OverrideName ?? fixture.Create<string>().Trim();
            return new Role(id, name);
        }));
    }
}