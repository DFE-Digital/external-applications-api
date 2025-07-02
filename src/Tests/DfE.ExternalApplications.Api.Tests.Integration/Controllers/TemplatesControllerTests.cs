using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TemplatesControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ShouldReturnLatest_WhenAzureTokenValid(
       CustomWebApplicationDbContextFactory<Program> factory,
       ITemplatesClient templatesClient,
       HttpClient httpClient)
    {
        var externalId = Guid.NewGuid();

        var dbContext = factory.GetDbContext<ExternalApplicationsContext>();
        var template = await dbContext.Templates.FirstAsync();

        factory.TestClaims = new List<Claim>
        {
            new Claim("iss", "windows.net"),
            new Claim("appid", externalId.ToString()),
            new Claim(ClaimTypes.Email, "bob@example.com"),
            new Claim(ClaimTypes.Role, "API.Read"),
            new Claim("permission", $"Template:{template.Id.Value}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "azure-token");

        await dbContext.Users
            .Where(u => u.Email == "bob@example.com")
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.ExternalProviderId, externalId.ToString()));
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var latestVersion = await dbContext.TemplateVersions
            .Where(tv => tv.TemplateId == template.Id)
            .OrderByDescending(tv => tv.CreatedOn).FirstAsync();

        var newVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id,
            "v9.9",
            "{\"new\":true}",
            DateTime.UtcNow.AddMinutes(1),
            latestVersion.CreatedBy);

        dbContext.TemplateVersions.Add(newVersion);
        await dbContext.SaveChangesAsync();

        var response = await templatesClient.GetLatestTemplateSchemaAsync(template.Id.Value);

        Assert.NotNull(response);
        Assert.Equal("v9.9", response!.VersionNumber);
        Assert.Equal("{\"new\":true}", response.JsonSchema);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ShouldReturnLatest_WhenUserTokenValid(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient,
        HttpClient httpClient)
    {
        factory.TestClaims = new List<Claim>
        {
            new Claim("permission", "Template:" +
            // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
            factory.GetDbContext<ExternalApplicationsContext>().Templates.First().Id!.Value + ":Read"),
            new Claim(ClaimTypes.Email, "bob@example.com")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var dbContext = factory.GetDbContext<ExternalApplicationsContext>();
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var template = await dbContext.Templates.FirstAsync();
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var latestVersion = await dbContext.TemplateVersions
            .Where(tv => tv.TemplateId == template.Id)
            .OrderByDescending(tv => tv.CreatedOn).FirstAsync();

        var newVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id!,
            "v9.9",
            "{\"new\":true}",
            DateTime.UtcNow.AddMinutes(1),
            latestVersion.CreatedBy);

        dbContext.TemplateVersions.Add(newVersion);
        await dbContext.SaveChangesAsync();

        var response = await templatesClient.GetLatestTemplateSchemaAsync(template.Id!.Value);

        Assert.NotNull(response);
        Assert.Equal("v9.9", response!.VersionNumber);
        Assert.Equal("{\"new\":true}", response.JsonSchema);
    }


}
