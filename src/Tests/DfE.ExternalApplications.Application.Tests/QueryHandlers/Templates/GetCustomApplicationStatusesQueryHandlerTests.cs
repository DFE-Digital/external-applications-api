using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

public class GetCustomApplicationStatusesQueryHandlerTests
{
    [Theory]
    [CustomAutoData]
    public async Task Handle_ReturnsAllApplicationStatuses_WithCustomLabels(
        Guid templateId)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();

        var existingStatuses = new List<CustomApplicationStatus>
        {
            new CustomApplicationStatus(
                new CustomApplicationStatusId(Guid.NewGuid()),
                new TemplateId(templateId),
                ApplicationStatus.Submitted,
                "Custom Submitted Label",
                DateTime.UtcNow,
                new UserId(Guid.NewGuid())
            ),
            new CustomApplicationStatus(
                new CustomApplicationStatusId(Guid.NewGuid()),
                new TemplateId(templateId),
                ApplicationStatus.InProgress,
                "Custom In Progress Label",
                DateTime.UtcNow,
                new UserId(Guid.NewGuid())
            )
        };

        var queryable = existingStatuses.AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(queryable);

        var handler = new GetCustomApplicationStatusesQueryHandler(customStatusRepo);
        var query = new GetCustomApplicationStatusesQuery(templateId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // Should return entries for all ApplicationStatus enum values
        var allStatuses = Enum.GetValues(typeof(ApplicationStatus)).Cast<ApplicationStatus>();
        Assert.Equal(allStatuses.Count(), result.Value.Count);

        // Check custom labels are present
        var submittedStatus = result.Value.First(s => s.ApplicationStatus == ApplicationStatus.Submitted);
        Assert.Equal("Custom Submitted Label", submittedStatus.Label);

        var inProgressStatus = result.Value.First(s => s.ApplicationStatus == ApplicationStatus.InProgress);
        Assert.Equal("Custom In Progress Label", inProgressStatus.Label);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ReturnsAllApplicationStatuses_WhenNoCustomStatusesExist(
        Guid templateId)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();

        var emptyList = new List<CustomApplicationStatus>();
        var queryable = emptyList.AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(queryable);

        var handler = new GetCustomApplicationStatusesQueryHandler(customStatusRepo);
        var query = new GetCustomApplicationStatusesQuery(templateId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // Should still return entries for all ApplicationStatus enum values
        var allStatuses = Enum.GetValues(typeof(ApplicationStatus)).Cast<ApplicationStatus>();
        Assert.Equal(allStatuses.Count(), result.Value.Count);

        // All labels should be null when no custom statuses exist
        Assert.All(result.Value, status => Assert.Null(status.Label));
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ReturnsFailure_WhenExceptionOccurs(
        Guid templateId)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();
        customStatusRepo.Query().Returns(_ => throw new Exception("Database error"));

        var handler = new GetCustomApplicationStatusesQueryHandler(customStatusRepo);
        var query = new GetCustomApplicationStatusesQuery(templateId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database error", result.Error);
    }
}
