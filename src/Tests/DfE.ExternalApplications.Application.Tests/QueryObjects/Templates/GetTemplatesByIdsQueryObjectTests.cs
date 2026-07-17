using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using FluentAssertions;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Templates;

public class GetTemplatesByIdsQueryObjectTests
{
    [Fact]
    public void Apply_ShouldReturnMatchingTemplates_OrderedByName()
    {
        var userId = new UserId(Guid.NewGuid());
        var idA = new TemplateId(Guid.NewGuid());
        var idB = new TemplateId(Guid.NewGuid());
        var idC = new TemplateId(Guid.NewGuid());

        var templates = new List<Template>
        {
            new(idB, "Beta", DateTime.UtcNow, userId),
            new(idA, "Alpha", DateTime.UtcNow, userId),
            new(idC, "Gamma", DateTime.UtcNow, userId)
        };

        var queryObject = new GetTemplatesByIdsQueryObject(new[] { idA, idB });
        var result = queryObject.Apply(templates.AsQueryable().BuildMock()).ToList();

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().ContainInOrder("Alpha", "Beta");
        result.Should().NotContain(t => t.Id == idC);
    }

    [Fact]
    public void Apply_ShouldReturnEmpty_WhenNoIdsProvided()
    {
        var userId = new UserId(Guid.NewGuid());
        var templates = new List<Template>
        {
            new(new TemplateId(Guid.NewGuid()), "Alpha", DateTime.UtcNow, userId)
        };

        var queryObject = new GetTemplatesByIdsQueryObject(Array.Empty<TemplateId>());
        var result = queryObject.Apply(templates.AsQueryable().BuildMock()).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_ShouldIncludeTemplateVersions()
    {
        var userId = new UserId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        var template = new Template(templateId, "Alpha", DateTime.UtcNow, userId);
        template.AddVersion(new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            userId));

        var queryObject = new GetTemplatesByIdsQueryObject(new[] { templateId });
        var result = queryObject.Apply(new List<Template> { template }.AsQueryable().BuildMock()).Single();

        result.TemplateVersions.Should().HaveCount(1);
        result.TemplateVersions.Single().VersionNumber.Should().Be("1.0.0");
    }
}

public class GetAllTemplateIdsQueryObjectTests
{
    [Fact]
    public void Apply_ShouldReturnAllTemplateIds()
    {
        var userId = new UserId(Guid.NewGuid());
        var idA = new TemplateId(Guid.NewGuid());
        var idB = new TemplateId(Guid.NewGuid());

        var templates = new List<Template>
        {
            new(idA, "Alpha", DateTime.UtcNow, userId),
            new(idB, "Beta", DateTime.UtcNow, userId)
        };

        var result = new GetAllTemplateIdsQueryObject()
            .Apply(templates.AsQueryable().BuildMock())
            .ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(idA);
        result.Should().Contain(idB);
    }

    [Fact]
    public void Apply_ShouldReturnEmpty_WhenNoTemplatesExist()
    {
        var result = new GetAllTemplateIdsQueryObject()
            .Apply(new List<Template>().AsQueryable().BuildMock())
            .ToList();

        result.Should().BeEmpty();
    }
}
