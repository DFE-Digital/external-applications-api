using DfE.ExternalApplications.Application.Notifications.Queries;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Notifications;

public class GetNotificationsByCategoryQueryValidatorTests
{
    private readonly GetNotificationsByCategoryQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenCategoryIsValid()
    {
        var query = new GetNotificationsByCategoryQuery("TestCategory");

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenCategoryIsEmpty(string? category)
    {
        var query = new GetNotificationsByCategoryQuery(category!);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Category);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCategoryExceedsMaxLength()
    {
        var query = new GetNotificationsByCategoryQuery(new string('a', 101));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Category);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenCategoryIsExactly100Characters()
    {
        var query = new GetNotificationsByCategoryQuery(new string('a', 100));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenUnreadOnlyIsTrue()
    {
        var query = new GetNotificationsByCategoryQuery("TestCategory", true);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
