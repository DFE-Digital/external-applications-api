using DfE.ExternalApplications.Application.Users.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Users;

public class ExchangeTokenQueryValidatorTests
{
    [Theory]
    [InlineData("valid-token")]
    public void Validate_ShouldSucceed_ForValidToken(string token)
    {
        var query = new ExchangeTokenQuery(token);
        var validator = new ExchangeTokenQueryValidator();

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void Validate_ShouldFail_ForInvalidToken(string token)
    {
        var query = new ExchangeTokenQuery(token);
        var validator = new ExchangeTokenQueryValidator();

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(query.SubjectToken));
    }
} 