using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class DeleteFileCommandValidatorTests
{
    private readonly DeleteFileCommandValidator _validator = new();

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Command(Guid fileId, Guid applicationId)
    {
        // Arrange
        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_FileId_Is_Empty(Guid applicationId)
    {
        // Arrange
        var command = new DeleteFileCommand(Guid.Empty, applicationId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_ApplicationId_Is_Empty(Guid fileId)
    {
        // Arrange
        var command = new DeleteFileCommand(fileId, Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Both_Ids_Are_Empty()
    {
        // Arrange
        var command = new DeleteFileCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

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
        var applicationId = Guid.NewGuid();
        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
} 