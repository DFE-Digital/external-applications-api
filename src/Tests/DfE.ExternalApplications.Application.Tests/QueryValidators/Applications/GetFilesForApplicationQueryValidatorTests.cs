using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.ValueObjects;
using FluentValidation.TestHelper;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GetFilesForApplicationQueryValidatorTests
{
    private readonly GetFilesForApplicationQueryValidator _validator = new();

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Query(ApplicationId applicationId)
    {
        // Arrange
        var query = new GetFilesForApplicationQuery(applicationId);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_ApplicationId_Is_Null()
    {
        // Arrange
        var query = new GetFilesForApplicationQuery(null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_ApplicationId_Is_Valid()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var query = new GetFilesForApplicationQuery(applicationId);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
} 