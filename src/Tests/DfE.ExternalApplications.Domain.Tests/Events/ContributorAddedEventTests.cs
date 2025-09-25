using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using System;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Events;

public class ContributorAddedEventTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Create_Event_With_Valid_Parameters(
        ApplicationId applicationId,
        string applicationReference,
        TemplateId templateId,
        Domain.Entities.User contributor,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(applicationId, applicationReference, templateId, contributor, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateId, @event.TemplateId);
        Assert.Equal(contributor, @event.Contributor);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Allow_Null_ApplicationId(
        string applicationReference,
        TemplateId templateId,
        Domain.Entities.User contributor,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(null!, applicationReference, templateId, contributor, addedBy, addedOn);

        // Assert
        Assert.Null(@event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateId, @event.TemplateId);
        Assert.Equal(contributor, @event.Contributor);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Allow_Null_ApplicationReference(
        ApplicationId applicationId,
        TemplateId templateId,
        Domain.Entities.User contributor,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(applicationId, null!, templateId, contributor, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Null(@event.ApplicationReference);
        Assert.Equal(templateId, @event.TemplateId);
        Assert.Equal(contributor, @event.Contributor);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Allow_Null_TemplateId(
        ApplicationId applicationId,
        string applicationReference,
        Domain.Entities.User contributor,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(applicationId, applicationReference, null!, contributor, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Null(@event.TemplateId);
        Assert.Equal(contributor, @event.Contributor);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Allow_Null_Contributor(
        ApplicationId applicationId,
        string applicationReference,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(applicationId, applicationReference, templateId, null!, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateId, @event.TemplateId);
        Assert.Null(@event.Contributor);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_Should_Allow_Null_AddedBy(
        ApplicationId applicationId,
        string applicationReference,
        TemplateId templateId,
        Domain.Entities.User contributor,
        DateTime addedOn)
    {
        // Act
        var @event = new ContributorAddedEvent(applicationId, applicationReference, templateId, contributor, null!, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(applicationReference, @event.ApplicationReference);
        Assert.Equal(templateId, @event.TemplateId);
        Assert.Equal(contributor, @event.Contributor);
        Assert.Null(@event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }
}