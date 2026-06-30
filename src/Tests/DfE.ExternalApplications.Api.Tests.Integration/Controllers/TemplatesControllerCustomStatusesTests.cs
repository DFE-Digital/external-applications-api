using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Http.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.Mocks.WebApplicationFactory;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using System.Net.Http.Headers;
using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TemplatesControllerCustomStatusesTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetCustomApplicationStatusesAsync_ShouldReturnAllStatuses_WhenAuthenticated(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new(ClaimTypes.Role, "Admin")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var result = await templatesClient.GetCustomApplicationStatusesAsync(Guid.Parse(EaContextSeeder.TemplateId));

        // Assert
        Assert.NotNull(result);

        // Should return all ApplicationStatus enum values
        var allStatuses = Enum.GetValues(typeof(ApplicationStatus)).Cast<ApplicationStatus>();
        Assert.Equal(allStatuses.Count(), result.Count);

        // Verify all status values are present
        foreach (var status in allStatuses)
        {
            Assert.Contains(result, s => s.ApplicationStatus == ApplicationStatus.Submitted);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetCustomApplicationStatusesAsync_ShouldReturnForbidden_WhenNotAuthenticated(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.GetCustomApplicationStatusesAsync(Guid.Parse(EaContextSeeder.TemplateId)));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateCustomApplicationStatusAsync_ShouldCreateNewStatus_WhenNotExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new(ClaimTypes.Role, "Admin")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new CustomApplicationStatusDto
        {
            ApplicationStatus = ApplicationStatus.Submitted,
            Label = "Custom Rejection Label"
        };

        // Act
        var result = await templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ApplicationStatus.Submitted, result.ApplicationStatus);
        Assert.Equal("Custom Rejection Label", result.Label);
        Assert.NotEqual(Guid.Empty, result.CustomApplicationStatusId);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateCustomApplicationStatusAsync_ShouldUpdateExistingStatus_WhenExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new(ClaimTypes.Role, "Admin")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Create first
        var request1 = new CustomApplicationStatusDto
        {
            ApplicationStatus = ApplicationStatus.InProgress,
            Label = "First Label"
        };

        var firstResult = await templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request1);
        var firstId = firstResult.CustomApplicationStatusId;

        // Update with new label
        var request2 = new CustomApplicationStatusDto
        {
            ApplicationStatus = ApplicationStatus.InProgress,
            Label = "Updated Label"
        };

        // Act
        var secondResult = await templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request2);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(firstId, secondResult.CustomApplicationStatusId); // Same ID
        Assert.Equal(ApplicationStatus.InProgress, secondResult.ApplicationStatus);
        Assert.Equal("Updated Label", secondResult.Label);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateCustomApplicationStatusAsync_ShouldAllowNullLabel(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new(ClaimTypes.Role, "Admin")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new CustomApplicationStatusDto
        {
            ApplicationStatus = ApplicationStatus.Submitted,
            Label = null
        };

        // Act
        var result = await templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ApplicationStatus.Submitted, result.ApplicationStatus);
        Assert.Null(result.Label);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateCustomApplicationStatusAsync_ShouldReturnForbidden_WhenNotAdmin(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        // Arrange - User without Admin role
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new CustomApplicationStatusDto
        {
            ApplicationStatus = ApplicationStatus.Submitted,
            Label = "Test Label"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateCustomApplicationStatusAsync_ShouldReturnForbidden_WhenNotAuthenticated(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        CustomApplicationStatusDto request)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => templatesClient.CreateCustomApplicationStatusAsync(Guid.Parse(EaContextSeeder.TemplateId), request));
        Assert.Equal(403, ex.StatusCode);
    }
}
