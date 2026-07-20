using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.Queries;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using FluentValidation.TestHelper;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryValidators.Applications;

public class DownloadFileQueryValidatorTests
{
    private readonly DownloadFileQueryValidator _validator = new();

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Query(Guid fileId, ApplicationId applicationId)
    {
        // Arrange
        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_FileId_Is_Empty(ApplicationId applicationId)
    {
        // Arrange
        var query = new DownloadFileQuery(Guid.Empty, applicationId);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_ApplicationId_Is_Empty(Guid fileId)
    {
        // Arrange
        var query = new DownloadFileQuery(fileId, null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Both_Ids_Are_Invalid()
    {
        // Arrange
        var query = new DownloadFileQuery(Guid.Empty, null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId);
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Guids_Provided()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var applicationId = new ApplicationId(Guid.NewGuid());
        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
} 