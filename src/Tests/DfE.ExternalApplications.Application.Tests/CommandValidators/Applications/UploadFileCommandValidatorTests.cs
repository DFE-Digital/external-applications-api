using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.ValueObjects;
using FluentValidation.TestHelper;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class UploadFileCommandValidatorTests
{
    private readonly UploadFileCommandValidator _validator = new();

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Command(
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_ApplicationId_Is_Null(
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(null!, name, description, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Name_Is_Empty(
        ApplicationId applicationId,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, "", description, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Name_Is_Null(
        ApplicationId applicationId,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, null!, description, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Name_Exceeds_Maximum_Length(
        ApplicationId applicationId,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var longName = new string('a', 256); // 256 characters exceeds the 255 limit
        var command = new UploadFileCommand(applicationId, longName, description, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Description_Is_Null(
        ApplicationId applicationId,
        string name,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, name, null, originalFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_OriginalFileName_Is_Empty(
        ApplicationId applicationId,
        string name,
        string description,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, name, description, "", fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalFileName);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_OriginalFileName_Is_Null(
        ApplicationId applicationId,
        string name,
        string description,
        Stream fileContent)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, name, description, null!, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalFileName);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_OriginalFileName_Exceeds_Maximum_Length(
        ApplicationId applicationId,
        string name,
        string description,
        Stream fileContent)
    {
        // Arrange
        var longFileName = new string('a', 256); // 256 characters exceeds the 255 limit
        var command = new UploadFileCommand(applicationId, name, description, longFileName, fileContent);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalFileName);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_FileContent_Is_Null(
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName)
    {
        // Arrange
        var command = new UploadFileCommand(applicationId, name, description, originalFileName, null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileContent);
    }
} 