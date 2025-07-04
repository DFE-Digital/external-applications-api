using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Factories;

public class ApplicationFactoryTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void AddResponseToApplication_ShouldAddNewResponse_WhenValidParameters(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        string responseBody,
        UserId addedBy)
    {
        // Arrange
        var factory = new ApplicationFactory();
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act
        var result = factory.AddResponseToApplication(application, responseBody, addedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(responseBody, result.ResponseBody);
        Assert.Equal(addedBy, result.CreatedBy);
        Assert.Equal(applicationId, result.ApplicationId);
        Assert.Single(application.Responses);
        Assert.Equal(addedBy, application.LastModifiedBy);
        Assert.NotNull(application.LastModifiedOn);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void AddResponseToApplication_ShouldRaiseDomainEvent_WhenResponseAdded(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        string responseBody,
        UserId addedBy)
    {
        // Arrange
        var factory = new ApplicationFactory();
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act
        factory.AddResponseToApplication(application, responseBody, addedBy);

        // Assert
        var domainEvents = application.DomainEvents;
        Assert.Single(domainEvents);
        var domainEvent = domainEvents.First() as ApplicationResponseAddedEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(applicationId, domainEvent.ApplicationId);
        Assert.Equal(addedBy, domainEvent.AddedBy);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void AddResponseToApplication_ShouldThrowArgumentException_WhenResponseBodyIsEmpty(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        UserId addedBy)
    {
        // Arrange
        var factory = new ApplicationFactory();
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            factory.AddResponseToApplication(application, string.Empty, addedBy));

        Assert.Equal("responseBody", ex.ParamName);
        Assert.Contains("Response body cannot be null or empty", ex.Message);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void AddResponseToApplication_ShouldThrowArgumentNullException_WhenApplicationIsNull(
        string responseBody,
        UserId addedBy)
    {
        // Arrange
        var factory = new ApplicationFactory();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            factory.AddResponseToApplication(null!, responseBody, addedBy));

        Assert.Equal("application", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void AddResponseToApplication_ShouldThrowArgumentNullException_WhenAddedByIsNull(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        string responseBody)
    {
        // Arrange
        var factory = new ApplicationFactory();
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            factory.AddResponseToApplication(application, responseBody, null!));

        Assert.Equal("addedBy", ex.ParamName);
    }
} 