using DfE.ExternalApplications.Application.Templates.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Templates;

public class GetLatestTemplateSchemaQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldSucceed_ForValidData()
    {
        var query = new GetLatestTemplateSchemaQuery("Template", Guid.NewGuid());
        var validator = new GetLatestTemplateSchemaQueryValidator();

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemplateNameIsEmpty()
    {
        var query = new GetLatestTemplateSchemaQuery(string.Empty, Guid.NewGuid());
        var validator = new GetLatestTemplateSchemaQueryValidator();

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var query = new GetLatestTemplateSchemaQuery("Template", Guid.Empty);
        var validator = new GetLatestTemplateSchemaQueryValidator();

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}