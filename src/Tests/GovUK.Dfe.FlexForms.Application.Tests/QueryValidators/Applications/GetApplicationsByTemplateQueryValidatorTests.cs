using GovUK.Dfe.FlexForms.Application.Applications.Queries;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryValidators.Applications;

public class GetApplicationsByTemplateQueryValidatorTests
{
    private readonly GetApplicationsByTemplateQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenTemplateIdIsEmpty()
    {
        var result = _validator.Validate(new GetApplicationsByTemplateQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenTemplateIdIsProvided()
    {
        var result = _validator.Validate(new GetApplicationsByTemplateQuery(Guid.NewGuid()));
        Assert.True(result.IsValid);
    }
}
