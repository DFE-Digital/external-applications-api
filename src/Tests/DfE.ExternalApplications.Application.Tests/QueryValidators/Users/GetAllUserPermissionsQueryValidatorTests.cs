using DfE.ExternalApplications.Application.Users.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Users;

public class GetAllUserPermissionsQueryValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("TEST@EXAMPLE.COM")]
    public void Validate_ShouldSucceed_ForValidEmail(string email)
    {
        var query = new GetAllUserPermissionsQuery(email);
        var validator = new GetAllUserPermissionsQueryValidator();

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData(null)]
    public void Validate_ShouldFail_ForInvalidEmail(string? email)
    {
        var query = new GetAllUserPermissionsQuery(email!);
        var validator = new GetAllUserPermissionsQueryValidator();

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}