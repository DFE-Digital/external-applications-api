using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Infrastructure.Security;
using System.Net.Http.Headers;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers
{
    public class UsersControllerTests
    {
        [Theory]
        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
        public async Task GetAllPermissionsForUserAsync_ShouldReturnPermissions_WhenUserExists(
            CustomWebApplicationDbContextFactory<Program> factory,
            IUsersClient usersClient,
            HttpClient httpClient)
        {

            factory.TestClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "alice1@example.com"),
                new Claim(ClaimTypes.Role,  "API.Read"),
            };

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "user-token");
            httpClient.DefaultRequestHeaders.Remove(AuthConstants.ServiceAuthHeader);
            httpClient.DefaultRequestHeaders.Add(
                AuthConstants.ServiceAuthHeader,
                "svc-token"
            );
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
                .GetAllPermissionsForUserAsync("alice1@example.com");

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
