//using System.Net;
//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.Applications.Commands;
//using DfE.ExternalApplications.Domain.ValueObjects;
//using DfE.ExternalApplications.Tests.Common.Customizations;
//using DfE.ExternalApplications.Tests.Common.Seeders;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Security.Claims;
//using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
//using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;

//namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

//public class ApplicationsControllerTests
//{
//    [Theory]
//    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//    public async Task CreateApplicationAsync_ShouldCreateApplication_WhenValidRequest(
//        CustomWebApplicationDbContextFactory<Program> factory,
//        HttpClient httpClient,
//        string applicationReference,
//        string initialResponseBody)
//    {
//        // Arrange
//        factory.TestClaims = new List<Claim>
//        {
//            new(ClaimTypes.NameIdentifier, EaContextSeeder.BobId),
//            new("permission", "Application:Write")
//        };

//        httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", "test-token");

//        var command = new CreateApplicationCommand(
//            applicationReference,
//            new TemplateVersionId(Guid.Parse(EaContextSeeder.TemplateVersionId)),
//            initialResponseBody);

//        // Act
//        var response = await httpClient.PostAsJsonAsync("/v1/applications", command);

//        // Assert
//        response.EnsureSuccessStatusCode();
//        var result = await response.Content.ReadFromJsonAsync<ApplicationDto>();
//        Assert.NotNull(result);
//        Assert.Equal(applicationReference, result.ApplicationReference);
//    }

//    [Theory]
//    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//    public async Task CreateApplicationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
//        CustomWebApplicationDbContextFactory<Program> factory,
//        HttpClient httpClient,
//        CreateApplicationCommand command)
//    {
//        // Act
//        var response = await httpClient.PostAsJsonAsync("/v1/applications", command);

//        // Assert
//        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
//    }

//    [Theory]
//    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//    public async Task CreateApplicationAsync_ShouldReturnForbidden_WhenPermissionMissing(
//        CustomWebApplicationDbContextFactory<Program> factory,
//        HttpClient httpClient,
//        CreateApplicationCommand command)
//    {
//        // Arrange
//        factory.TestClaims = new List<Claim>
//        {
//            new(ClaimTypes.NameIdentifier, EaContextSeeder.BobId)
//        };

//        httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", "test-token");

//        // Act
//        var response = await httpClient.PostAsJsonAsync("/v1/applications", command);

//        // Assert
//        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
//    }

//    [Theory]
//    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
//    public async Task CreateApplicationAsync_ShouldReturnBadRequest_WhenInvalidData(
//        CustomWebApplicationDbContextFactory<Program> factory,
//        HttpClient httpClient)
//    {
//        // Arrange
//        factory.TestClaims = new List<Claim>
//        {
//            new(ClaimTypes.NameIdentifier, EaContextSeeder.BobId),
//            new("permission", "Application:Write")
//        };

//        httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", "test-token");

//        var command = new CreateApplicationCommand(
//            string.Empty,
//            new TemplateVersionId(Guid.Parse(EaContextSeeder.TemplateVersionId)),
//            string.Empty);

//        // Act
//        var response = await httpClient.PostAsJsonAsync("/v1/applications", command);

//        // Assert
//        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
//    }
//} 