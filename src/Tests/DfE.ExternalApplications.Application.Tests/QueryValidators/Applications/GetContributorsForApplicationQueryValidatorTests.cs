using DfE.ExternalApplications.Application.Applications.Queries;
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
    public void Validate_WithValidQuery_ShouldPass()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.NewGuid(), true);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyApplicationId_ShouldFail()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.Empty, true);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetContributorsForApplicationQuery.ApplicationId));
    }

    [Fact]
    public void Validate_WithDefaultIncludePermissionDetails_ShouldPass()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.NewGuid(), false);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var query = new GetContributorsForApplicationQuery(Guid.Empty, false);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetContributorsForApplicationQuery.ApplicationId));
    }
} 