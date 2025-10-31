using FluentValidation.TestHelper;
using DfE.ExternalApplications.Application.Applications.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GetApplicationsForUserByExternalProviderIdQueryValidatorTests
{
    private readonly GetApplicationsForUserByExternalProviderIdQueryValidator _validator;

    public GetApplicationsForUserByExternalProviderIdQueryValidatorTests()
    {
        _validator = new GetApplicationsForUserByExternalProviderIdQueryValidator();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenExternalProviderIdIsValid()
    {
        // Arrange
        var query = new GetApplicationsForUserByExternalProviderIdQuery("valid-external-id");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenExternalProviderIdIsEmpty(string? externalProviderId)
    {
        // Arrange
        var query = new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExternalProviderId)
            .WithErrorMessage("External Provider ID is required");
    }
}

