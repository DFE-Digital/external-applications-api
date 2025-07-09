using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using System.Net.Http.Headers;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TemplatesControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ShouldReturnSchema_WhenUserHasAccess(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new("appid", EaContextSeeder.BobExternalId),
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var result = await templatesClient.GetLatestTemplateSchemaAsync(Guid.Parse(EaContextSeeder.TemplateId));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(EaContextSeeder.TemplateVersionId), result.TemplateVersionId);
        Assert.NotNull(result.JsonSchema);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.GetLatestTemplateSchemaAsync(Guid.Parse(EaContextSeeder.TemplateId)));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new("appid", EaContextSeeder.BobExternalId),
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.GetLatestTemplateSchemaAsync(Guid.Parse(EaContextSeeder.TemplateId)));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldCreateVersion_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var jsonSchema = "{ \"new\": \"schema\" }";
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(jsonSchema);
        var base64JsonSchema = System.Convert.ToBase64String(plainTextBytes);
        
        var request = new CreateTemplateVersionRequest("1.0.1", base64JsonSchema);

        // Act
        var result = await templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.0.1", result.VersionNumber);
        Assert.Equal(jsonSchema, result.JsonSchema);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        CreateTemplateVersionRequest request)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient,
        CreateTemplateVersionRequest request)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(403, ex.StatusCode);
    }
    
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldReturnConflict_WhenVersionExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes("{ \"new\": \"schema\" }");
        var base64JsonSchema = System.Convert.ToBase64String(plainTextBytes);

        var request = new CreateTemplateVersionRequest("1.0.0", base64JsonSchema);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");
        
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes("{ \"new\": \"schema\" }");
        var base64JsonSchema = System.Convert.ToBase64String(plainTextBytes);

        var request = new CreateTemplateVersionRequest(string.Empty, base64JsonSchema);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateTemplateVersionAsync_ShouldReturnBadRequest_WhenSchemaIsNotBase64(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new CreateTemplateVersionRequest("1.0.1", "this is not base64");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateTemplateVersionAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(400, ex.StatusCode);
    }
}
