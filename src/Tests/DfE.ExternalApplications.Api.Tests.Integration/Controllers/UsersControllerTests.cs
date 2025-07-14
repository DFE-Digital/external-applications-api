using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers
{
    public class UsersControllerTests
    {
        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetAllMyPermissionsAsync_ShouldReturnPermissions_WhenUserExtAppIdExists(
            CustomWebApplicationDbContextFactory<Program> factory,
            IUsersClient usersClient,
            HttpClient httpClient)
        {
            var externalId = Guid.NewGuid();

            factory.TestClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Role,  "API.Read"),
                new Claim("iss",  "windows.net"),
                new Claim("appid",  externalId.ToString()),
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "azure-token");

            // Arrange
            var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

            await dbContext.Users
                .Where(x => x.Email == "alice@example.com")
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.ExternalProviderId, externalId.ToString()));

            var aliceId = dbContext.Users
                .Where(u => u.ExternalProviderId == externalId.ToString())
                .Select(u => u.Id)
                .Single();

            await dbContext.Permissions
                .Where(p => p.UserId == aliceId)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(p => p.ResourceKey, externalId.ToString())
                    .SetProperty(p => p.ResourceType, ResourceType.User)
                    .SetProperty(p => p.AccessType, AccessType.Read)
                );

            var expectedUser = dbContext.Users
                .Include(u => u.Permissions)
                .Include(u => u.Role)
                .FirstOrDefault(u => u.ExternalProviderId == externalId.ToString())!;

            var expectedPermissions = expectedUser.Permissions.ToList();

            var result = await usersClient.GetMyPermissionsAsync();

            // Assert: Check permissions
            Assert.NotNull(result);
            Assert.NotNull(result.Permissions);
            Assert.Equal(expectedPermissions.Count, result.Permissions.Count());

            var actual = result.Permissions
                .Select(dto => (dto.ResourceKey, dto.AccessType))
                .OrderBy(x => x.ResourceKey)
                .ThenBy(x => x.AccessType)
                .ToList();

            // Compare element by element
            for (int i = 0; i < expectedPermissions.Count; i++)
            {
                Assert.Equal(expectedPermissions[i].ResourceKey, actual[i].ResourceKey);
                Assert.Equal((AccessType)expectedPermissions[i].AccessType, actual[i].AccessType);
            }

            // Assert: Check roles
            Assert.NotNull(result.Roles);
            if (expectedUser.Role != null)
            {
                Assert.Single(result.Roles);
                Assert.Equal(expectedUser.Role.Name, result.Roles.First());
            }
            else
            {
                Assert.Empty(result.Roles);
            }
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetAllMyPermissionsAsync_ShouldReturnPermissions_WhenUserEmailExists(
         CustomWebApplicationDbContextFactory<Program> factory,
         IUsersClient usersClient,
         HttpClient httpClient)
        {

            factory.TestClaims = new List<Claim>
            {
                new Claim("permission",  "User:alice1@example.com:Read"),
                new Claim(ClaimTypes.Email, "alice1@example.com"),
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "user-token");

            // Arrange
            var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

            await dbContext.Users
                .Where(x => x.Email == "alice@example.com")
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.Email, "alice1@example.com"));

            var aliceId = dbContext.Users
                .Where(u => u.Email == "alice1@example.com")
                .Select(u => u.Id)
                .Single();

            await dbContext.Permissions
                .Where(p => p.UserId == aliceId)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(p => p.ResourceKey, "alice1@example.com")
                    .SetProperty(p => p.ResourceType, ResourceType.User)
                    .SetProperty(p => p.AccessType, AccessType.Read)
                );

            var expectedUser = dbContext.Users
                .Include(u => u.Permissions)
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == "alice1@example.com")!;

            var expectedPermissions = expectedUser.Permissions.ToList();

            var result = await usersClient.GetMyPermissionsAsync();

            // Assert: Check permissions
            Assert.NotNull(result);
            Assert.NotNull(result.Permissions);
            Assert.Equal(expectedPermissions.Count, result.Permissions.Count());

            var actual = result.Permissions
                .Select(dto => (dto.ResourceKey, dto.AccessType))
                .OrderBy(x => x.ResourceKey)
                .ThenBy(x => x.AccessType)
                .ToList();

            // Compare element by element
            for (int i = 0; i < expectedPermissions.Count; i++)
            {
                Assert.Equal(expectedPermissions[i].ResourceKey, actual[i].ResourceKey);
                Assert.Equal((AccessType)expectedPermissions[i].AccessType, actual[i].AccessType);
            }

            // Assert: Check roles
            Assert.NotNull(result.Roles);
            if (expectedUser.Role != null)
            {
                Assert.Single(result.Roles);
                Assert.Equal(expectedUser.Role.Name, result.Roles.First());
            }
            else
            {
                Assert.Empty(result.Roles);
            }
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetMyPermissionsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
     CustomWebApplicationDbContextFactory<Program> factory,
     IUsersClient usersClient)
        {
            var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
                () => usersClient.GetMyPermissionsAsync());

            Assert.Equal(403, ex.StatusCode);
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetMyPermissionsAsync_ShouldReturnForbidden_WhenPermissionMissing(
            CustomWebApplicationDbContextFactory<Program> factory,
            IUsersClient usersClient,
            HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "user-token");

            var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
                () => usersClient.GetMyPermissionsAsync());

            Assert.Equal(403, ex.StatusCode);
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetMyApplicationsAsync_ShouldReturnApplications_WhenUserEmailExists(
          CustomWebApplicationDbContextFactory<Program> factory,
          IApplicationsClient appsClient,
          HttpClient httpClient)
        {
            factory.TestClaims = new List<Claim>
            {
                new Claim("permission", $"Application:{EaContextSeeder.ApplicationId}:Read"),
                new Claim(ClaimTypes.Email, "alice@example.com")
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "user-token");

            var list = await appsClient.GetMyApplicationsAsync();

            Assert.NotNull(list);
            Assert.NotEmpty(list!);
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetApplicationsForUserAsync_ShouldReturnApplications_WhenAuthorized(
            CustomWebApplicationDbContextFactory<Program> factory,
            IApplicationsClient appsClient,
            HttpClient httpClient)
        {
            factory.TestClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "API.Read"),
                new Claim("permission", $"Application:{EaContextSeeder.ApplicationId}:Read")
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "azure-token");

            var list = await appsClient.GetApplicationsForUserAsync("bob@example.com");

            Assert.NotNull(list);
            Assert.NotEmpty(list!);
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetMyApplicationsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
            CustomWebApplicationDbContextFactory<Program> factory,
            IApplicationsClient appsClient
            )
        {
            var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
                () => appsClient.GetMyApplicationsAsync(includeSchema: null));
            Assert.Equal(403, ex.StatusCode);
        }

        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetApplicationsForUserAsync_ShouldReturnForbidden_WhenPermissionMissing(
            CustomWebApplicationDbContextFactory<Program> factory,
            IApplicationsClient appsClient,
            HttpClient httpClient)
        {
            factory.TestClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "API.Read"),
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "azure-token");

            var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
                () => appsClient.GetApplicationsForUserAsync("bob@example.com", includeSchema: null));

            Assert.Equal(403, ex.StatusCode);
        }
    }
}
