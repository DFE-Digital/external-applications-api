using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using FluentAssertions;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Templates
{
    public class GetTemplateByIdQueryObjectTests
    {
        [Fact]
        public void Apply_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());
            var templateIdToFind = new TemplateId(Guid.NewGuid());

            var templates = new List<Template>
            {
                new(new TemplateId(Guid.NewGuid()), "Template 1", DateTime.UtcNow, userId),
                new(templateIdToFind, "Template 2", DateTime.UtcNow, userId),
                new(new TemplateId(Guid.NewGuid()), "Template 3", DateTime.UtcNow, userId)
            };
            var mockQuery = templates.AsQueryable().BuildMock();
            var queryObject = new GetTemplateByIdQueryObject(templateIdToFind);

            // Act
            var result = queryObject.Apply(mockQuery).ToList();

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(templateIdToFind);
            result.First().Name.Should().Be("Template 2");
        }
    }
} 