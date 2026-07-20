using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.Services.RoleProvisioners;
using GovUK.Dfe.FlexForms.Domain.Factories;
using NSubstitute;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Services;

public class UserRoleProvisionerRegistryTests
{
    [Fact]
    public void GetProvisioner_ShouldResolveCaseworkerProvisioner()
    {
        var userFactory = new UserFactory();
        var registry = new UserRoleProvisionerRegistry([
            new CaseworkerRoleProvisioner(userFactory),
            new StandardUserRoleProvisioner(userFactory),
            new AdminRoleProvisioner(userFactory)
        ]);

        var provisioner = registry.GetProvisioner("caseworker");

        Assert.NotNull(provisioner);
        Assert.Equal(RoleNames.Caseworker, provisioner!.RoleName);
        Assert.True(provisioner.RequiresTemplateIds);
    }

    [Fact]
    public void GetProvisioner_ShouldReturnNull_ForUnknownRole()
    {
        var userFactory = Substitute.For<IUserFactory>();
        var registry = new UserRoleProvisionerRegistry([
            new AdminRoleProvisioner(userFactory)
        ]);

        Assert.Null(registry.GetProvisioner("Unknown"));
    }
}
