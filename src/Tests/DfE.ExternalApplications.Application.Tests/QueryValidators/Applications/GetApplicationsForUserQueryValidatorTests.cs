using DfE.ExternalApplications.Application.Applications.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GetApplicationsForUserQueryValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("TEST@EXAMPLE.COM")]
    public void Validate_ShouldSucceed_ForValidEmail(string email)
    {
        var query = new GetApplicationsForUserQuery(email);
        var validator = new GetApplicationsForUserQueryValidator();

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData(null)]
    public void Validate_ShouldFail_ForInvalidEmail(string? email)
    {
        var query = new GetApplicationsForUserQuery(email!);
        var validator = new GetApplicationsForUserQueryValidator();

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}