using AutoFixture;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Helpers;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users
{
    public class GetUserWithAllPermissionsQueryObjectTests
    {

        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization))]
        public void Apply_ShouldReturnOnlyMatchingUser_WithAllPermissions(
            string rawEmail,
            UserCustomization userCustom,
            PermissionCustomization permCustom)
        {
            // Normalize email
            var normalizedEmail = rawEmail.Trim().ToLowerInvariant();

            userCustom.OverrideEmail = rawEmail;
            userCustom.OverridePermissions = Array.Empty<Permission>();
            var fixtureUserA = new AutoFixture.Fixture().Customize(userCustom);
            var userA = fixtureUserA.Create<User>();


            var grantedBy = new UserId(Guid.NewGuid());
            userA.AddPermission(
                new ApplicationId(Guid.NewGuid()),
                "Resource:Read",
                ResourceType.Field,
                AccessType.Read,
                grantedBy,
                DateTime.UtcNow);
            userA.AddPermission(
                new ApplicationId(Guid.NewGuid()),
                "Resource:Write",
                ResourceType.Field,
                AccessType.Write,
                grantedBy,
                DateTime.UtcNow);

            var userCustomB = new UserCustomization();
            var fixtureUserB = new AutoFixture.Fixture().Customize(userCustomB);
            userCustomB.OverrideEmail = "otheruser@example.com";
            var userB = fixtureUserB.Create<User>();

            var backingFieldB = typeof(User)
                .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            backingFieldB.SetValue(userB, new List<Permission>());

            userB.AddPermission(
                new ApplicationId(Guid.NewGuid()),
                "Resource:Delete",
                ResourceType.Field,
                AccessType.Write,
                grantedBy,
                DateTime.UtcNow);

            using var dbContext = CreateAndSeedSqliteContext(ctx =>
            {
                ctx.Users.Add(userA);
                ctx.Users.Add(userB);
                ctx.SaveChanges();
            });


            var baseQuery = dbContext.Users.AsQueryable();
            var sut = new GetUserWithAllPermissionsQueryObject(rawEmail);
            var result = sut.Apply(baseQuery).ToList();

            Assert.Single(result);

            var fetchedUser = result[0];
            Assert.Equal(normalizedEmail, fetchedUser.Email.ToLowerInvariant());
            Assert.Equal(2, fetchedUser.Permissions.Count);

            var resourceKeys = fetchedUser.Permissions
                .Select(p => p.ResourceKey)
                .OrderBy(r => r)
                .ToList();
            Assert.Equal(new[] { "Resource:Read", "Resource:Write" }, resourceKeys);
        }

        [Theory, CustomAutoData(typeof(UserCustomization))]
        public void Apply_ShouldReturnEmpty_IfNoMatchingUser(
            string rawEmail,
            UserCustomization userCustom)
        {
            userCustom.OverrideEmail = "alice@domain.com";
            var fixtureUser = new AutoFixture.Fixture().Customize(userCustom);
            var user = fixtureUser.Create<User>();

            var perm = user.AddPermission(
                new ApplicationId(Guid.NewGuid()),
                "Some:Key",
                ResourceType.Field,
                AccessType.Read,
                new UserId(Guid.NewGuid()),
                DateTime.UtcNow);

            using var dbContext = CreateAndSeedSqliteContext(ctx =>
            {
                ctx.Users.Add(user);
                ctx.Permissions.Add(perm);
                ctx.SaveChanges();
            });

            var nonExistentEmail = rawEmail.Trim().ToLowerInvariant();
            var baseQuery = dbContext.Users.AsQueryable();
            var sut = new GetUserWithAllPermissionsQueryObject(nonExistentEmail);

            var result = sut.Apply(baseQuery).ToList();
            Assert.Empty(result);
        }

        private ExternalApplicationsContext CreateAndSeedSqliteContext(
            Action<ExternalApplicationsContext> seedTestData)
        {
            var services = new ServiceCollection();

            var dummyConfig = Substitute.For<IConfiguration>();
            services.AddSingleton<IConfiguration>(dummyConfig);

            var dummyMediator = Substitute.For<IMediator>();
            services.AddSingleton<IMediator>(dummyMediator);

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using (var disableFk = connection.CreateCommand())
            {
                disableFk.CommandText = "PRAGMA foreign_keys = OFF;";
                disableFk.ExecuteNonQuery();
            }

            DbContextHelper.CreateDbContext<ExternalApplicationsContext>(
                services,
                connection,
                seedTestData);

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ExternalApplicationsContext>();
        }
    }
}