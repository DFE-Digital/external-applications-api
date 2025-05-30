//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
//using DfE.ExternalApplications.Client.Contracts;
//using DfE.ExternalApplications.Infrastructure.Database;
//using DfE.ExternalApplications.Tests.Common.Customizations;
//using DfE.ExternalApplications.Tests.Common.Customizations.Commands;
//using DfE.ExternalApplications.Tests.Common.Customizations.Models;
//using Microsoft.EntityFrameworkCore;
//using System.Net;
//using System.Security.Claims;

//namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers
//{
//    public class SchoolsControllerTests
//    {
//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//        public async Task GetPrincipalBySchoolAsync_ShouldReturnPrincipal_WhenSchoolExists(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            ISchoolsClient schoolsClient)
//        {
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];

//            // Arrange
//            var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

//            await dbContext.Schools
//                .Where(x => x.SchoolName == "Test School 1")
//                .ExecuteUpdateAsync(x => x.SetProperty(p => p.SchoolName, "NewSchoolName"));

//            var schoolName = Uri.EscapeDataString("NewSchoolName");

//            // Act
//            var result = await schoolsClient.GetPrincipalBySchoolAsync(schoolName);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("NewSchoolName", result.SchoolName);
//        }

//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//        public async Task GetPrincipalBySchoolAsync_ShouldReturnNotFound_WhenSchoolDoesNotExist(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            ISchoolsClient schoolsClient)
//        {
//            // Arrange
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];

//            var schoolName = Uri.EscapeDataString("NonExistentSchool");

//            // Act & Assert
//            var exception = await Assert.ThrowsAsync<PersonsApiException>(async () =>
//                await schoolsClient.GetPrincipalBySchoolAsync(schoolName));

//            Assert.Equal(HttpStatusCode.NotFound, (HttpStatusCode)exception.StatusCode);
//        }

//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//        public async Task GetPrincipalsBySchoolsAsync_ShouldReturnPrincipals_WhenSchoolsExists(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            ISchoolsClient schoolsClient)
//        {
//            // Arrange
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];

//            var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

//            await dbContext.Schools.Where(x => x.SchoolName == "Test School 1")
//                .ExecuteUpdateAsync(x => x.SetProperty(p => p.SchoolName, "NewSchoolName"));

//            // Act
//            var result = await schoolsClient.GetPrincipalsBySchoolsAsync(
//                new GetPrincipalsBySchoolsQuery() { SchoolNames = ["NewSchoolName", "Test School 2"] });

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEmpty(result);
//            Assert.Equal(2, result.Count);
//        }

//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//        public async Task GetPrincipalsBySchoolsAsync_ShouldReturnEmpty_WhenSchoolsDontExists(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            ISchoolsClient schoolsClient)
//        {
//            // Arrange
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];

//            // Act
//            var result = await schoolsClient.GetPrincipalsBySchoolsAsync(
//                new GetPrincipalsBySchoolsQuery() { SchoolNames = ["NewSchoolName1"] });

//            // Assert
//            Assert.NotNull(result);
//            Assert.Empty(result);
//        }

//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//        public async Task GetPrincipalsBySchoolsAsync_ShouldThrowAnException_WhenSchoolsNotProvided(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            ISchoolsClient schoolsClient)
//        {
//            // Arrange
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];

//            // Act & Assert
//            var exception = await Assert.ThrowsAsync<PersonsApiException>(async () =>
//                await schoolsClient.GetPrincipalsBySchoolsAsync(
//                    new GetPrincipalsBySchoolsQuery() { SchoolNames = [] }));

//            Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)exception.StatusCode);
//        }

//        [Theory]
//        [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization),
//            typeof(PrincipalDetailsApiClientCustomization),
//            typeof(CreateSchoolCommandApiClientCustomization))]
//        public async Task CreateSchoolAsync_ShouldReturnSchoolId_WhenValidRequest(
//            CustomWebApplicationDbContextFactory<Program> factory,
//            CreateSchoolCommand command,
//            ISchoolsClient schoolsClient)
//        {
//            // Arrange
//            factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Write"), new Claim(ClaimTypes.Role, "API.Read")];

//            // Act
//            var result = await schoolsClient.CreateSchoolAsync(command);

//            // Assert
//            Assert.NotNull(result);
//            Assert.IsType<int>(result.Value);
//        }
//    }
//}
