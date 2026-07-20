using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Events;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using System;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Events;

public class ApplicationCreatedEventTests
{
    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Create_Event_With_Valid_Parameters(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        UserId createdBy,
        DateTime createdOn)
    {
        // Act
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, templateVersionId, createdBy, createdOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateVersionId, @event.TemplateVersionId);
        Assert.Equal(createdBy, @event.CreatedBy);
        Assert.Equal(createdOn, @event.CreatedOn);
        Assert.Equal(createdOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_ApplicationId(
        string applicationReference,
        TemplateVersionId templateVersionId,
        UserId createdBy,
        DateTime createdOn)
    {
        // Act
        var @event = new ApplicationCreatedEvent(null!, applicationReference, templateVersionId, createdBy, createdOn);

        // Assert
        Assert.Null(@event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateVersionId, @event.TemplateVersionId);
        Assert.Equal(createdBy, @event.CreatedBy);
        Assert.Equal(createdOn, @event.CreatedOn);
        Assert.Equal(createdOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_ApplicationReference(
        ApplicationId applicationId,
        TemplateVersionId templateVersionId,
        UserId createdBy,
        DateTime createdOn)
    {
        // Act
        var @event = new ApplicationCreatedEvent(applicationId, null!, templateVersionId, createdBy, createdOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Null(@event.ApplicationReference);
        Assert.Equal(templateVersionId, @event.TemplateVersionId);
        Assert.Equal(createdBy, @event.CreatedBy);
        Assert.Equal(createdOn, @event.CreatedOn);
        Assert.Equal(createdOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_TemplateVersionId(
        ApplicationId applicationId,
        string applicationReference,
        UserId createdBy,
        DateTime createdOn)
    {
        // Act
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, null!, createdBy, createdOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Null(@event.TemplateVersionId);
        Assert.Equal(createdBy, @event.CreatedBy);
        Assert.Equal(createdOn, @event.CreatedOn);
        Assert.Equal(createdOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_CreatedBy(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn)
    {
        // Act
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, templateVersionId, null!, createdOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateVersionId, @event.TemplateVersionId);
        Assert.Null(@event.CreatedBy);
        Assert.Equal(createdOn, @event.CreatedOn);
        Assert.Equal(createdOn, @event.OccurredOn);
    }
} 