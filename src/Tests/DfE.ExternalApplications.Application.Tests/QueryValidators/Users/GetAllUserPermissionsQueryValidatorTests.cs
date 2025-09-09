using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.ValueObjects;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Users;

public class GetAllUserPermissionsQueryValidatorTests
{
    private readonly GetAllUserPermissionsQueryValidator _validator = new();

    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Valid_Query(UserId email)
    {
        // Arrange
        var query = new GetAllUserPermissionsQuery(email);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // Arrange
        var query = new GetAllUserPermissionsQuery(null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [CustomAutoData]
    public void Should_Fail_When_Email_Is_Null()
    {
        // Arrange
        var query = new GetAllUserPermissionsQuery(null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }


    [Theory]
    [CustomAutoData]
    public void Should_Pass_When_Email_Is_Valid()
    {
        // Arrange
        var query = new GetAllUserPermissionsQuery(new UserId(Guid.NewGuid()));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
} 