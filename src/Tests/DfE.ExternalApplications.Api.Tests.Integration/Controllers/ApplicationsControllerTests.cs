using System.Net;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using System.Net.Http.Headers;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class ApplicationsControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldCreateApplication_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string initialResponseBody)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new("appid", EaContextSeeder.BobExternalId),
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", "Application:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new CreateApplicationRequest
        {
            TemplateId = Guid.Parse(EaContextSeeder.TemplateVersionId),
            InitialResponseBody = initialResponseBody
        };

        // Act
        var result = await applicationsClient.CreateApplicationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("APP-", result.ApplicationReference);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        CreateApplicationRequest request)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        CreateApplicationRequest request)
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
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new("appid", EaContextSeeder.BobExternalId),
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", "Application:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new CreateApplicationRequest
        {
            TemplateId = Guid.Parse(EaContextSeeder.TemplateVersionId),
            InitialResponseBody = string.Empty
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnApplications_WhenUserHasAccess(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new("appid", EaContextSeeder.BobExternalId),
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", "Application:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var result = await applicationsClient.GetMyApplicationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetMyApplicationsAsync());
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
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
            () => applicationsClient.GetMyApplicationsAsync());
        Assert.Equal(403, ex.StatusCode);
    }
} 