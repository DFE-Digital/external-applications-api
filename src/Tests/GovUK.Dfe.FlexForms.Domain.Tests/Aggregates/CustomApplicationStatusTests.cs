using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Aggregates;

public class CustomApplicationStatusTests
{
    [Theory]
    [CustomAutoData]
    public void Constructor_ShouldCreateInstance_WithValidParameters(
        CustomApplicationStatusId id,
        TemplateId templateId,
        ApplicationStatus applicationStatus,
        string label,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act
        var customStatus = new CustomApplicationStatus(
            id,
            templateId,
            applicationStatus,
            label,
            createdOn,
            createdBy);

        // Assert
        Assert.Equal(id, customStatus.Id);
        Assert.Equal(templateId, customStatus.TemplateId);
        Assert.Equal(applicationStatus, customStatus.ApplicationStatus);
        Assert.Equal(label, customStatus.Label);
        Assert.Equal(createdOn, customStatus.CreatedOn);
        Assert.Equal(createdBy, customStatus.CreatedBy);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_ShouldAcceptNullLabel(
        CustomApplicationStatusId id,
        TemplateId templateId,
        ApplicationStatus applicationStatus,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act
        var customStatus = new CustomApplicationStatus(
            id,
            templateId,
            applicationStatus,
            null,
            createdOn,
            createdBy);

        // Assert
        Assert.Null(customStatus.Label);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        TemplateId templateId,
        ApplicationStatus applicationStatus,
        string label,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CustomApplicationStatus(
                null!,
                templateId,
                applicationStatus,
                label,
                createdOn,
                createdBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData]
    public void Constructor_ShouldThrowArgumentNullException_WhenTemplateIdIsNull(
        CustomApplicationStatusId id,
        ApplicationStatus applicationStatus,
        string label,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CustomApplicationStatus(
                id,
                null!,
                applicationStatus,
                label,
                createdOn,
                createdBy));

        Assert.Equal("templateId", ex.ParamName);
    }

    [Theory]
    [CustomAutoData]
    public void UpdateLabel_ShouldUpdateLabelValue(
        CustomApplicationStatusId id,
        TemplateId templateId,
        ApplicationStatus applicationStatus,
        string initialLabel,
        string newLabel,
        DateTime createdOn,
        UserId createdBy)
    {
        // Arrange
        var customStatus = new CustomApplicationStatus(
            id,
            templateId,
            applicationStatus,
            initialLabel,
            createdOn,
            createdBy);

        // Act
        customStatus.UpdateLabel(newLabel);

        // Assert
        Assert.Equal(newLabel, customStatus.Label);
    }

    [Theory]
    [CustomAutoData]
    public void UpdateLabel_ShouldAcceptNullValue(
        CustomApplicationStatusId id,
        TemplateId templateId,
        ApplicationStatus applicationStatus,
        string initialLabel,
        DateTime createdOn,
        UserId createdBy)
    {
        // Arrange
        var customStatus = new CustomApplicationStatus(
            id,
            templateId,
            applicationStatus,
            initialLabel,
            createdOn,
            createdBy);

        // Act
        customStatus.UpdateLabel(null);

        // Assert
        Assert.Null(customStatus.Label);
    }
}
