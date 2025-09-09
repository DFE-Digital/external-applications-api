//using AutoFixture;
//using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects;
//using DfE.ExternalApplications.Domain.Entities;
//using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
//using Microsoft.Data.Sqlite;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using NSubstitute;
//using DfE.ExternalApplications.Infrastructure.Database;
//using MediatR;
//using GovUK.Dfe.CoreLibs.Testing.Helpers;

//namespace DfE.ExternalApplications.Application.Tests.QueryObjects.TemplatePermissions;

//public class GetTemplatePermissionsForUserQueryObjectTests
//{
//    [Theory, CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
//    public void Apply_ShouldReturnOnlyMatchingUser_WithAllTemplatePermissions(
//        string rawEmail,
//        UserCustomization userCustom,
//        TemplatePermissionCustomization tpCustom)
//    {
//        var normalized = rawEmail.Trim().ToLowerInvariant();

//        userCustom.OverrideEmail = rawEmail;
//        userCustom.OverridePermissions = Array.Empty<Permission>();
//        var fixtureUserA = new Fixture().Customize(userCustom);
//        var userA = fixtureUserA.Create<User>();

//        var backingField = typeof(User)
//            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
//        backingField.SetValue(userA, new List<TemplatePermission>());

//        var tp = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
//        ((List<TemplatePermission>)backingField.GetValue(userA)!).Add(tp);

//        var userCustomB = new UserCustomization { OverrideEmail = "other@example.com" };
//        var userB = new Fixture().Customize(userCustomB).Create<User>();
//        backingField.SetValue(userB, new List<TemplatePermission>());

//        using var context = CreateAndSeedSqliteContext(ctx =>
//        {
//            ctx.Users.Add(userA);
//            ctx.Users.Add(userB);
//            ctx.SaveChanges();
//        });

//        var queryable = context.Users.AsQueryable();
//        var sut = new GetTemplatePermissionsForUserQueryObject(rawEmail);
//        var result = sut.Apply(queryable).ToList();

//        Assert.Single(result);
//        Assert.Equal(normalized, result[0].Email.ToLowerInvariant());
//        Assert.Single(result[0].TemplatePermissions);
//    }

//    [Theory, CustomAutoData(typeof(UserCustomization))]
//    public void Apply_ShouldReturnEmpty_IfNoMatchingUser(
//        string rawEmail,
//        UserCustomization userCustom)
//    {
//        userCustom.OverrideEmail = "someone@domain.com";
//        var fixture = new Fixture().Customize(userCustom);
//        var user = fixture.Create<User>();

//        var backingField = typeof(User)
//            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
//        backingField.SetValue(user, new List<TemplatePermission>());

//        using var context = CreateAndSeedSqliteContext(ctx =>
//        {
//            ctx.Users.Add(user);
//            ctx.SaveChanges();
//        });

//        var queryable = context.Users.AsQueryable();
//        var sut = new GetTemplatePermissionsForUserQueryObject(rawEmail);
//        var result = sut.Apply(queryable).ToList();
//        Assert.Empty(result);
//    }

//    private ExternalApplicationsContext CreateAndSeedSqliteContext(Action<ExternalApplicationsContext> seed)
//    {
//        var services = new ServiceCollection();
//        var dummyConfig = Substitute.For<IConfiguration>();
//        services.AddSingleton<IConfiguration>(dummyConfig);
//        var dummyMediator = Substitute.For<IMediator>();
//        services.AddSingleton<IMediator>(dummyMediator);

//        var connection = new SqliteConnection("DataSource=:memory:");
//        connection.Open();
//        using (var disable = connection.CreateCommand())
//        {
//            disable.CommandText = "PRAGMA foreign_keys = OFF;";
//            disable.ExecuteNonQuery();
//        }

//        DbContextHelper.CreateDbContext<ExternalApplicationsContext>(services, connection, seed);
//        var provider = services.BuildServiceProvider();
//        var scope = provider.CreateScope();
//        return scope.ServiceProvider.GetRequiredService<ExternalApplicationsContext>();
//    }
//}