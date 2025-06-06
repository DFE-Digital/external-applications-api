using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TemplatesControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ReturnsLatestSchema_WhenUserHasAccess(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient client)
    {
        var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

        var template = await dbContext.Templates.FirstAsync();
        var userAccess = await dbContext.UserTemplateAccesses.FirstAsync();

        // add a newer version
        var latestVersion = await dbContext.TemplateVersions
            .Where(tv => tv.TemplateId == template.Id)
            .OrderByDescending(tv => tv.CreatedOn)
            .FirstAsync();

        var newVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id,
            versionNumber: "v9.9",
            jsonSchema: "{\"new\":true}",
            createdOn: DateTime.UtcNow.AddMinutes(1),
            createdBy: latestVersion.CreatedBy);

        dbContext.TemplateVersions.Add(newVersion);
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"v1/Templates/{Uri.EscapeDataString(template.Name)}/schema/{userAccess.UserId.Value}");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DfE.ExternalApplications.Application.Templates.Models.TemplateSchemaDto>();

        Assert.NotNull(dto);
        Assert.Equal("v9.9", dto!.VersionNumber);
        Assert.Equal("{\"new\":true}", dto.JsonSchema);
    }
}
