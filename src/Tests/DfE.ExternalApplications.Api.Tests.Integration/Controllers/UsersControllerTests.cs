using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
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

            var expected = dbContext.Users
                .Include(u => u.Permissions)
                .FirstOrDefault(u => u.ExternalProviderId == externalId.ToString())!
                .Permissions
                .ToList();

            var resultCollection = await usersClient
                .GetMyPermissionsAsync();

            var actual = resultCollection
                .Select(dto => (dto.ResourceKey, dto.AccessType))
                .OrderBy(x => x.ResourceKey)
                .ThenBy(x => x.AccessType)
                .ToList();

            // Assert: both lists have same count
            Assert.NotNull(resultCollection);
            Assert.Equal(expected.Count, actual.Count);

            // Compare element by element
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].ResourceKey, actual[i].ResourceKey);
                Assert.Equal((AccessType)expected[i].AccessType, actual[i].AccessType);
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

            var expected = dbContext.Users
                .Include(u => u.Permissions)
                .FirstOrDefault(u => u.Email == "alice1@example.com")!
                .Permissions
                .ToList();

            var resultCollection = await usersClient
                .GetMyPermissionsAsync();

            var actual = resultCollection
                .Select(dto => (dto.ResourceKey, dto.AccessType))
                .OrderBy(x => x.ResourceKey)
                .ThenBy(x => x.AccessType)
                .ToList();

            // Assert: both lists have same count
            Assert.NotNull(resultCollection);
            Assert.Equal(expected.Count, actual.Count);

            // Compare element by element
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].ResourceKey, actual[i].ResourceKey);
                Assert.Equal((AccessType)expected[i].AccessType, actual[i].AccessType);
            }
        }
    }
}
