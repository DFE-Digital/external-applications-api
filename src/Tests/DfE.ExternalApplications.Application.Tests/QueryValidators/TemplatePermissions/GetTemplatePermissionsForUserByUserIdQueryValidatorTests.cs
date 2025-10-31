using FluentValidation.TestHelper;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.TemplatePermissions;

public class GetTemplatePermissionsForUserByUserIdQueryValidatorTests
{
    private readonly GetTemplatePermissionsForUserByUserIdQueryValidator _validator;

    public GetTemplatePermissionsForUserByUserIdQueryValidatorTests()
    {
        _validator = new GetTemplatePermissionsForUserByUserIdQueryValidator();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenUserIdIsValid()
    {
        // Arrange
        var query = new GetTemplatePermissionsForUserByUserIdQuery(new UserId(Guid.NewGuid()));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsNull()
    {
        // Arrange
        var query = new GetTemplatePermissionsForUserByUserIdQuery(null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var query = new GetTemplatePermissionsForUserByUserIdQuery(new UserId(Guid.Empty));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }
}

