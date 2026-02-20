using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Services;

public class ApplicationResponseAppenderTests
{
    private readonly ApplicationResponseAppender _appender = new();

    [Fact]
    public void Create_ShouldReturnResult_WithResponse_WhenInputIsValid()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());
        var responseBody = "Test response body";
        var now = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        var result = _appender.Create(applicationId, responseBody, createdBy, now);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Response);
        Assert.Equal(applicationId, result.Response.ApplicationId);
        Assert.Equal(responseBody, result.Response.ResponseBody);
        Assert.Equal(createdBy, result.Response.CreatedBy);
        Assert.Equal(now, result.Response.CreatedOn);
    }

    [Fact]
    public void Create_ShouldReturnResult_WithDomainEvent()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());
        var now = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        var result = _appender.Create(applicationId, "body", createdBy, now);

        // Assert
        Assert.NotNull(result.DomainEvent);
        Assert.IsType<ApplicationResponseAddedEvent>(result.DomainEvent);
        Assert.Equal(applicationId, result.DomainEvent.ApplicationId);
        Assert.Equal(createdBy, result.DomainEvent.AddedBy);
        Assert.Equal(now, result.DomainEvent.AddedOn);
    }

    [Fact]
    public void Create_ShouldSetTimestampOnResult()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());
        var now = new DateTime(2024, 6, 1, 12, 0, 0);

        // Act
        var result = _appender.Create(applicationId, "body", createdBy, now);

        // Assert
        Assert.Equal(now, result.Now);
    }

    [Fact]
    public void Create_ShouldUseUtcNow_WhenNowNotProvided()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());
        var before = DateTime.UtcNow;

        // Act
        var result = _appender.Create(applicationId, "body", createdBy);
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.Now, before, after);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueResponseId()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());

        // Act
        var result1 = _appender.Create(applicationId, "body1", createdBy);
        var result2 = _appender.Create(applicationId, "body2", createdBy);

        // Assert
        Assert.NotEqual(result1.Response.Id, result2.Response.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrowArgumentException_WhenResponseBodyIsNullOrWhitespace(string? responseBody)
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdBy = new UserId(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _appender.Create(applicationId, responseBody!, createdBy));
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenCreatedByIsNull()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _appender.Create(applicationId, "body", null!));
    }
}
