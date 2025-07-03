using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using System.Net.Http.Headers;
using System.Security.Claims;

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
}
