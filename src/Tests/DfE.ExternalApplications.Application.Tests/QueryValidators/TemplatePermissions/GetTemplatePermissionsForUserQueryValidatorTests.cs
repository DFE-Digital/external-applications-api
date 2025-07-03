//using DfE.ExternalApplications.Application.TemplatePermissions.Queries;

//namespace DfE.ExternalApplications.Application.Tests.QueryValidators.TemplatePermissions;

//public class GetTemplatePermissionsForUserQueryValidatorTests
//{
//    [Theory]
//    [InlineData("user@example.com")]
//    [InlineData("TEST@EXAMPLE.COM")]
//    public void Validate_ShouldSucceed_ForValidEmail(string email)
//    {
//        var query = new GetTemplatePermissionsForUserQuery(email);
//        var validator = new GetTemplatePermissionsForUserQueryValidator();

//        var result = validator.Validate(query);

//        Assert.True(result.IsValid);
//    }

//    [Theory]
//    [InlineData("")]
//    [InlineData("not-an-email")]
//    [InlineData(null)]
//    public void Validate_ShouldFail_ForInvalidEmail(string? email)
//    {
//        var query = new GetTemplatePermissionsForUserQuery(email!);
//        var validator = new GetTemplatePermissionsForUserQueryValidator();

//        var result = validator.Validate(query);

//        Assert.False(result.IsValid);
//    }
//}