using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.ExternalApplications.Infrastructure.Database;
using MediatR;
using GovUK.Dfe.CoreLibs.Testing.Helpers;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.TemplatePermissions;

public class GetTemplatePermissionsForUserByExternalProviderIdQueryObjectTests
{
    [Theory, CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnOnlyMatchingUser_WithAllTemplatePermissions(
        string externalId,
        UserCustomization userCustom,
        TemplatePermissionCustomization tpCustom)
    {
        var sharedRoleId = new RoleId(Guid.NewGuid());
        userCustom.OverrideExternalProviderId = externalId;
        userCustom.OverrideRoleId = sharedRoleId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixtureUserA = new Fixture().Customize(userCustom);
        var userA = fixtureUserA.Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(userA, new List<TemplatePermission>());

        var tp = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        ((List<TemplatePermission>)backingField.GetValue(userA)!).Add(tp);

        var userCustomB = new UserCustomization { OverrideExternalProviderId = "other-id", OverrideRoleId = sharedRoleId };
        var userB = new Fixture().Customize(userCustomB).Create<User>();
        backingField.SetValue(userB, new List<TemplatePermission>());

        using var context = CreateAndSeedSqliteContext(ctx =>
        {
            ctx.Roles.Add(new Role(sharedRoleId, "TestRole"));
            ctx.Users.Add(userA);
            ctx.Users.Add(userB);
            ctx.SaveChanges();
        });

        var queryable = context.Users.AsQueryable();
        var sut = new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(externalId);
        var result = sut.Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(externalId, result[0].ExternalProviderId);
        Assert.Single(result[0].TemplatePermissions);
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_IfNoMatchingUser(
        string externalId,
        UserCustomization userCustom)
    {
        userCustom.OverrideExternalProviderId = "different";
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(user, new List<TemplatePermission>());

        using var context = CreateAndSeedSqliteContext(ctx =>
        {
            ctx.Roles.Add(new Role(user.RoleId, "TestRole"));
            ctx.Users.Add(user);
            ctx.SaveChanges();
        });

        var queryable = context.Users.AsQueryable();
        var sut = new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(externalId);
        var result = sut.Apply(queryable).ToList();
        Assert.Empty(result);
    }

    private ExternalApplicationsContext CreateAndSeedSqliteContext(Action<ExternalApplicationsContext> seed)
    {
        var services = new ServiceCollection();
        var dummyConfig = Substitute.For<IConfiguration>();
        services.AddSingleton<IConfiguration>(dummyConfig);
        var dummyMediator = Substitute.For<IMediator>();
        services.AddSingleton<IMediator>(dummyMediator);

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var disable = connection.CreateCommand())
        {
            disable.CommandText = "PRAGMA foreign_keys = OFF;";
            disable.ExecuteNonQuery();
        }

        DbContextHelper.CreateDbContext<ExternalApplicationsContext>(services, connection, seed);
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ExternalApplicationsContext>();
    }
}