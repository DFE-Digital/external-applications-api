using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Events;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using System;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Events;

public class ApplicationResponseAddedEventTests
{
    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Create_Event_With_Valid_Parameters(
        ApplicationId applicationId,
        ResponseId responseId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ApplicationResponseAddedEvent(applicationId, responseId, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(responseId, @event.ResponseId);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_ApplicationId(
        ResponseId responseId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ApplicationResponseAddedEvent(null!, responseId, addedBy, addedOn);

        // Assert
        Assert.Null(@event.ApplicationId);
        Assert.Equal(responseId, @event.ResponseId);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_ResponseId(
        ApplicationId applicationId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Act
        var @event = new ApplicationResponseAddedEvent(applicationId, null!, addedBy, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Null(@event.ResponseId);
        Assert.Equal(addedBy, @event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_AddedBy(
        ApplicationId applicationId,
        ResponseId responseId,
        DateTime addedOn)
    {
        // Act
        var @event = new ApplicationResponseAddedEvent(applicationId, responseId, null!, addedOn);

        // Assert
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(responseId, @event.ResponseId);
        Assert.Null(@event.AddedBy);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }
} 