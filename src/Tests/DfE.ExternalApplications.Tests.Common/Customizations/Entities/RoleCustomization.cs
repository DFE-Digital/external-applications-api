using AutoFixture;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

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