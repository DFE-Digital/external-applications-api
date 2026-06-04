using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class ApplicationListingQueryBuilderTests
{
    [Fact]
    public void ApplicationAccessResolver_ReturnsAllApplicationsInTenant_ForAdmin_ButMyApplicationsQueryIgnoresRole()
    {
        var userId = new UserId(Guid.NewGuid());
        var admin = new User(
            userId,
            new RoleId(RoleConstants.AdminRoleId),
            "Admin",
            "admin@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        admin.GetType().GetProperty(nameof(User.Role))!.SetValue(admin,
            new Role(new RoleId(RoleConstants.AdminRoleId), RoleNames.Admin));

        var scope = ApplicationAccessResolver.Resolve(admin);
        Assert.Equal(ApplicationAccessResolver.AccessMode.AllApplicationsInTenant, scope.Mode);

        var permissions = admin.GetType()
            .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        permissions.SetValue(admin, new List<Permission>());

        var appRepo = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable());

        var query = ApplicationListingQueryBuilder.BuildMyApplicationsQuery(appRepo, admin, Array.Empty<TemplateId>());

        Assert.NotNull(query);
    }
}
