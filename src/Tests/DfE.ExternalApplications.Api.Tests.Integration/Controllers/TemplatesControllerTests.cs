using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TemplatesControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetLatestTemplateSchemaAsync_ReturnsLatestSchema_WhenUserHasAccess(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITemplatesClient templatesClient)
    {
        var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var template = await dbContext.Templates.FirstAsync();
        var userAccess = await dbContext.TemplatePermissions.FirstAsync();

        // add a newer version
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var latestVersion = await dbContext.TemplateVersions
            .Where(tv => tv.TemplateId == template.Id)
            .OrderByDescending(tv => tv.CreatedOn).FirstAsync();

        var newVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id,
            versionNumber: "v9.9",
            jsonSchema: "{\"new\":true}",
            createdOn: DateTime.UtcNow.AddMinutes(1),
            createdBy: latestVersion.CreatedBy);

        dbContext.TemplateVersions.Add(newVersion);
        await dbContext.SaveChangesAsync();

        var response = await templatesClient.GetLatestTemplateSchemaAsync(template.Id.Value);

        Assert.NotNull(response);
        Assert.Equal("v9.9", response!.VersionNumber);
        Assert.Equal("{\"new\":true}", response.JsonSchema);
    }
}
