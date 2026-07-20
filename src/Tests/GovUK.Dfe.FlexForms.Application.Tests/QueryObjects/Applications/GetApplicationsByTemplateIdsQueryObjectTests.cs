using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Applications;

public class GetApplicationsByTemplateIdsQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldFilterByTemplateIds_WhenMultipleTemplatesProvided(ApplicationCustomization appCustom)
    {
        var firstTemplateId = new TemplateId(Guid.NewGuid());
        var secondTemplateId = new TemplateId(Guid.NewGuid());
        var otherTemplateId = new TemplateId(Guid.NewGuid());

        var fixture = new Fixture().Customize(appCustom);
        var createdBy = new UserId(Guid.NewGuid());

        var firstApp = CreateApplication(fixture, firstTemplateId, createdBy);
        var secondApp = CreateApplication(fixture, secondTemplateId, createdBy);
        var otherApp = CreateApplication(fixture, otherTemplateId, createdBy);

        var queryable = new[] { firstApp, secondApp, otherApp }.AsQueryable().BuildMock();
        var queryObject = new GetApplicationsByTemplateIdsQueryObject([firstTemplateId, secondTemplateId]);

        var result = queryObject.Apply(queryable).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(firstApp, result);
        Assert.Contains(secondApp, result);
        Assert.DoesNotContain(otherApp, result);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoTemplateIdsProvided(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var app = CreateApplication(fixture, new TemplateId(Guid.NewGuid()), new UserId(Guid.NewGuid()));
        var queryable = new[] { app }.AsQueryable().BuildMock();

        var queryObject = new GetApplicationsByTemplateIdsQueryObject(Array.Empty<TemplateId>());

        var result = queryObject.Apply(queryable).ToList();

        Assert.Empty(result);
    }

    private static Domain.Entities.Application CreateApplication(
        IFixture fixture,
        TemplateId templateId,
        UserId createdBy)
    {
        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            createdBy);

        var application = fixture.Create<Domain.Entities.Application>();
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(application, templateVersion);

        return application;
    }
}
