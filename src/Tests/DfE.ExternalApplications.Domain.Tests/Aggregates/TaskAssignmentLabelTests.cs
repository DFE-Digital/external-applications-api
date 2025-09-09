using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using System;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class TaskAssignmentLabelTests
{
    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Create_TaskAssignmentLabel_With_Valid_Parameters(
        TaskAssignmentLabelId id,
        string value,
        string taskId,
        UserId createdBy,
        DateTime createdOn,
        UserId userId)
    {
        // Act
        var taskAssignmentLabel = new TaskAssignmentLabel(id, value, taskId, createdBy, createdOn, userId);

        // Assert
        Assert.Equal(id, taskAssignmentLabel.Id);
        Assert.Equal(value, taskAssignmentLabel.Value);
        Assert.Equal(taskId, taskAssignmentLabel.TaskId);
        Assert.Equal(createdBy, taskAssignmentLabel.CreatedBy);
        Assert.Equal(createdOn, taskAssignmentLabel.CreatedOn);
        Assert.Equal(userId, taskAssignmentLabel.UserId);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Throw_ArgumentNullException_When_Id_Is_Null(
        string value,
        string taskId,
        UserId createdBy,
        DateTime createdOn,
        UserId userId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TaskAssignmentLabel(null!, value, taskId, createdBy, createdOn, userId));
        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Throw_ArgumentNullException_When_Value_Is_Null(
        TaskAssignmentLabelId id,
        string taskId,
        UserId createdBy,
        DateTime createdOn,
        UserId userId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TaskAssignmentLabel(id, null!, taskId, createdBy, createdOn, userId));
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Throw_ArgumentNullException_When_TaskId_Is_Null(
        TaskAssignmentLabelId id,
        string value,
        UserId createdBy,
        DateTime createdOn,
        UserId userId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TaskAssignmentLabel(id, value, null!, createdBy, createdOn, userId));
        Assert.Equal("taskId", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Allow_Null_CreatedBy(
        TaskAssignmentLabelId id,
        string value,
        string taskId,
        DateTime createdOn,
        UserId userId)
    {
        // Act
        var taskAssignmentLabel = new TaskAssignmentLabel(id, value, taskId, null, createdOn, userId);

        // Assert
        Assert.Equal(id, taskAssignmentLabel.Id);
        Assert.Equal(value, taskAssignmentLabel.Value);
        Assert.Equal(taskId, taskAssignmentLabel.TaskId);
        Assert.Null(taskAssignmentLabel.CreatedBy);
        Assert.Equal(createdOn, taskAssignmentLabel.CreatedOn);
        Assert.Equal(userId, taskAssignmentLabel.UserId);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Allow_Null_UserId(
        TaskAssignmentLabelId id,
        string value,
        string taskId,
        UserId createdBy,
        DateTime createdOn)
    {
        // Act
        var taskAssignmentLabel = new TaskAssignmentLabel(id, value, taskId, createdBy, createdOn, null);

        // Assert
        Assert.Equal(id, taskAssignmentLabel.Id);
        Assert.Equal(value, taskAssignmentLabel.Value);
        Assert.Equal(taskId, taskAssignmentLabel.TaskId);
        Assert.Equal(createdBy, taskAssignmentLabel.CreatedBy);
        Assert.Equal(createdOn, taskAssignmentLabel.CreatedOn);
        Assert.Null(taskAssignmentLabel.UserId);
    }

    [Theory]
    [CustomAutoData(typeof(TaskAssignmentLabelCustomization))]
    public void Constructor_Should_Use_Current_DateTime_When_CreatedOn_Is_Null(
        TaskAssignmentLabelId id,
        string value,
        string taskId,
        UserId createdBy,
        UserId userId)
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var taskAssignmentLabel = new TaskAssignmentLabel(id, value, taskId, createdBy, null, userId);

        // Assert
        Assert.Equal(id, taskAssignmentLabel.Id);
        Assert.Equal(value, taskAssignmentLabel.Value);
        Assert.Equal(taskId, taskAssignmentLabel.TaskId);
        Assert.Equal(createdBy, taskAssignmentLabel.CreatedBy);
        Assert.Equal(userId, taskAssignmentLabel.UserId);
        Assert.True(taskAssignmentLabel.CreatedOn >= beforeCreation);
        Assert.True(taskAssignmentLabel.CreatedOn <= DateTime.UtcNow);
    }
} 