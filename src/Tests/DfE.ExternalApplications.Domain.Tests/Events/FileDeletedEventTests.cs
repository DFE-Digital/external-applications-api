using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using System;

namespace DfE.ExternalApplications.Domain.Tests.Events;

public class FileDeletedEventTests
{
    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Create_Event_With_Valid_Parameters(
        FileId fileId,
        DateTime addedOn)
    {
        // Act
        var @event = new FileDeletedEvent(fileId, addedOn);

        // Assert
        Assert.Equal(fileId, @event.FileId);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_Should_Allow_Null_FileId(DateTime addedOn)
    {
        // Act
        var @event = new FileDeletedEvent(null!, addedOn);

        // Assert
        Assert.Null(@event.FileId);
        Assert.Equal(addedOn, @event.AddedOn);
        Assert.Equal(addedOn, @event.OccurredOn);
    }
} 