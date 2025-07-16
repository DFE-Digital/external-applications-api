using DfE.ExternalApplications.Application.Applications.Queries;
using FluentValidation.TestHelper;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GetContributorsForApplicationQueryValidatorTests
{
    private readonly GetContributorsForApplicationQueryValidator _validator;

    public GetContributorsForApplicationQueryValidatorTests()
    {
        _validator = new GetContributorsForApplicationQueryValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ApplicationId_Is_Empty()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }
} 