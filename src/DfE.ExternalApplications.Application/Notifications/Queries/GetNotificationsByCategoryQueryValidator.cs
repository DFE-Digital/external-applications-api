using FluentValidation;

namespace DfE.ExternalApplications.Application.Notifications.Queries;

public class GetNotificationsByCategoryQueryValidator : AbstractValidator<GetNotificationsByCategoryQuery>
{
    public GetNotificationsByCategoryQueryValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required")
            .MaximumLength(100)
            .WithMessage("Category cannot exceed 100 characters");
    }
}
