using FluentValidation;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

public class ClearNotificationsByContextCommandValidator : AbstractValidator<ClearNotificationsByContextCommand>
{
    public ClearNotificationsByContextCommandValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty()
            .WithMessage("Context is required")
            .MaximumLength(100)
            .WithMessage("Context cannot exceed 100 characters");
    }
}
