using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using FluentAssertions;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using MockQueryable;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Templates;

public class GetCustomApplicationStatusByTemplateIdAndApplicationStatusQueryObjectTests
{
    [Fact]
    public void Apply_ShouldReturnMatchingCustomApplicationStatus()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var userId = new UserId(Guid.NewGuid());

        var matchingStatus = new CustomApplicationStatus(
            new CustomApplicationStatusId(Guid.NewGuid()),
            new TemplateId(templateId),
            ApplicationStatus.Submitted,
            "Submitted Label",
            DateTime.UtcNow,
            userId);

        var otherStatusForTemplate = new CustomApplicationStatus(
            new CustomApplicationStatusId(Guid.NewGuid()),
            new TemplateId(templateId),
            ApplicationStatus.InProgress,
            "In Progress Label",
            DateTime.UtcNow,
            userId);

        var statusForOtherTemplate = new CustomApplicationStatus(
            new CustomApplicationStatusId(Guid.NewGuid()),
            new TemplateId(Guid.NewGuid()),
            ApplicationStatus.Submitted,
            "Other Template Label",
            DateTime.UtcNow,
            userId);

        var statuses = new List<CustomApplicationStatus>
        {
            matchingStatus,
            otherStatusForTemplate,
            statusForOtherTemplate
        };

        var mockQuery = statuses.AsQueryable().BuildMock();
        var queryObject = new GetCustomApplicationStatusByTemplateIdAndApplicationStatusQueryObject(
            templateId,
            ApplicationStatus.Submitted);

        // Act
        var result = queryObject.Apply(mockQuery).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(matchingStatus.Id);
        result.First().Label.Should().Be("Submitted Label");
    }

    [Fact]
    public void Apply_ShouldReturnEmpty_WhenNoMatchingCustomApplicationStatusExists()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var userId = new UserId(Guid.NewGuid());

        var statuses = new List<CustomApplicationStatus>
        {
            new(
                new CustomApplicationStatusId(Guid.NewGuid()),
                new TemplateId(templateId),
                ApplicationStatus.InProgress,
                "In Progress Label",
                DateTime.UtcNow,
                userId)
        };

        var mockQuery = statuses.AsQueryable().BuildMock();
        var queryObject = new GetCustomApplicationStatusByTemplateIdAndApplicationStatusQueryObject(
            templateId,
            ApplicationStatus.Submitted);

        // Act
        var result = queryObject.Apply(mockQuery).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
