using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class ApplicationTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        string reference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        ApplicationStatus? status,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Entities.Application(
                null!,       // id
                reference,
                templateVersionId,
                createdOn,
                createdBy,
                status,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenReferenceIsNull(
        ApplicationId id,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        ApplicationStatus? status,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Entities.Application(
                id,
                null!,       // reference
                templateVersionId,
                createdOn,
                createdBy,
                status,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("applicationReference", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void UpdateLastModified_ShouldUpdateTracking_WhenValidParameters(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime lastModifiedOn,
        UserId lastModifiedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act
        application.UpdateLastModified(lastModifiedOn, lastModifiedBy);

        // Assert
        Assert.Equal(lastModifiedOn, application.LastModifiedOn);
        Assert.Equal(lastModifiedBy, application.LastModifiedBy);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void UpdateLastModified_ShouldThrowArgumentNullException_WhenLastModifiedByIsNull(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime lastModifiedOn)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            application.UpdateLastModified(lastModifiedOn, null!));

        Assert.Equal("lastModifiedBy", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void GetLatestResponse_ShouldReturnNull_WhenNoResponsesExist(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Act
        var result = application.GetLatestResponse();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void GetLatestResponse_ShouldReturnLatestResponse_WhenMultipleResponsesExist(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        string firstResponseBody,
        string secondResponseBody,
        UserId responseCreatedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        // Add responses manually using the AddResponse method
        var firstResponse = new ApplicationResponse(
            new ResponseId(Guid.NewGuid()),
            applicationId,
            firstResponseBody,
            createdOn,
            responseCreatedBy);

        var secondResponse = new ApplicationResponse(
            new ResponseId(Guid.NewGuid()),
            applicationId,
            secondResponseBody,
            createdOn.AddMinutes(1), // Ensure different timestamp
            responseCreatedBy);

        application.AddResponse(firstResponse);
        application.AddResponse(secondResponse);

        // Act
        var result = application.GetLatestResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondResponse.Id, result.Id);
        Assert.Equal(secondResponseBody, result.ResponseBody);
        Assert.True(result.CreatedOn >= firstResponse.CreatedOn);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldUpdateStatusAndLastModified_WhenValidParameters(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn,
        UserId submittedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.InProgress); // Start with InProgress status

        // Act
        application.Submit(submittedOn, submittedBy, "test@example.com", "Test User");

        // Assert
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(submittedOn, application.LastModifiedOn);
        Assert.Equal(submittedBy, application.LastModifiedBy);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldThrowArgumentNullException_WhenSubmittedByIsNull(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.InProgress);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            application.Submit(submittedOn, null!, "test@example.com", "Test User"));

        Assert.Equal("submittedBy", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldThrowInvalidOperationException_WhenApplicationAlreadySubmitted(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn,
        UserId submittedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.Submitted); // Already submitted

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            application.Submit(submittedOn, submittedBy, "test@example.com", "Test User"));

        Assert.Equal("Application has already been submitted", ex.Message);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldWorkFromDifferentStatuses_WhenNotAlreadySubmitted(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn,
        UserId submittedBy)
    {
        // Test submitting from null status
        var applicationWithNullStatus = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            null); // null status

        applicationWithNullStatus.Submit(submittedOn, submittedBy, "test@example.com", "Test User");
        Assert.Equal(ApplicationStatus.Submitted, applicationWithNullStatus.Status);

        // Test submitting from InProgress status
        var applicationWithInProgress = new Entities.Application(
            new ApplicationId(Guid.NewGuid()),
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.InProgress);

        applicationWithInProgress.Submit(submittedOn, submittedBy, "test@example.com", "Test User");
        Assert.Equal(ApplicationStatus.Submitted, applicationWithInProgress.Status);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldThrowArgumentException_WhenUserEmailIsNullOrEmpty(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn,
        UserId submittedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.InProgress);

        // Act & Assert - Test null email
        var ex1 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, null!, "Test User"));
        Assert.Equal("userEmail", ex1.ParamName);

        // Act & Assert - Test empty email
        var ex2 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, "", "Test User"));
        Assert.Equal("userEmail", ex2.ParamName);

        // Act & Assert - Test whitespace email
        var ex3 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, "   ", "Test User"));
        Assert.Equal("userEmail", ex3.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Submit_ShouldThrowArgumentException_WhenUserFullNameIsNullOrEmpty(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        DateTime submittedOn,
        UserId submittedBy)
    {
        // Arrange
        var application = new Entities.Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy,
            ApplicationStatus.InProgress);

        // Act & Assert - Test null name
        var ex1 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, "test@example.com", null!));
        Assert.Equal("userFullName", ex1.ParamName);

        // Act & Assert - Test empty name
        var ex2 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, "test@example.com", ""));
        Assert.Equal("userFullName", ex2.ParamName);

        // Act & Assert - Test whitespace name
        var ex3 = Assert.Throws<ArgumentException>(() =>
            application.Submit(submittedOn, submittedBy, "test@example.com", "   "));
        Assert.Equal("userFullName", ex3.ParamName);
    }
}