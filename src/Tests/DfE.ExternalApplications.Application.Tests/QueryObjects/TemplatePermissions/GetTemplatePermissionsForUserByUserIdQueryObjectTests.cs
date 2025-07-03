using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.ExternalApplications.Infrastructure.Database;
using MediatR;
using DfE.CoreLibs.Testing.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.TemplatePermissions;

public class GetTemplatePermissionsForUserByUserIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnMatchingUser_WithAllTemplatePermissions(
        UserId userId,
        UserCustomization userCustom,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        userCustom.OverrideId = userId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixtureUserA = new Fixture().Customize(userCustom);
        var userA = fixtureUserA.Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(userA, new List<TemplatePermission>());

        var tp = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        ((List<TemplatePermission>)backingField.GetValue(userA)!).Add(tp);

        var userCustomB = new UserCustomization();
        var userB = new Fixture().Customize(userCustomB).Create<User>();
        backingField.SetValue(userB, new List<TemplatePermission>());

        using var context = CreateAndSeedSqliteContext(ctx =>
        {
            ctx.Users.Add(userA);
            ctx.Users.Add(userB);
            ctx.SaveChanges();
        });

        // Act
        var queryable = context.Users.AsQueryable();
        var sut = new GetTemplatePermissionsForUserByUserIdQueryObject(userId);
        var result = sut.Apply(queryable).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(userId, result[0].Id);
        Assert.Single(result[0].TemplatePermissions);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoUserMatches(
        UserId userId,
        UserCustomization userCustom)
    {
        // Arrange
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(user, new List<TemplatePermission>());

        using var context = CreateAndSeedSqliteContext(ctx =>
        {
            ctx.Users.Add(user);
            ctx.SaveChanges();
        });

        // Act
        var queryable = context.Users.AsQueryable();
        var sut = new GetTemplatePermissionsForUserByUserIdQueryObject(userId);
        var result = sut.Apply(queryable).ToList();

        // Assert
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
        return scope.ServiceProvider.GetRequiredService<ExternalApplicationsContext>(); ;

    }
} 