using FluentValidation;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

public class ClearNotificationsByCategoryCommandValidator : AbstractValidator<ClearNotificationsByCategoryCommand>
{
    public ClearNotificationsByCategoryCommandValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required")
            .MaximumLength(100)
            .WithMessage("Category cannot exceed 100 characters");
    }
}
