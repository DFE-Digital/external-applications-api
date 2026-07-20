using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.TemplatePermissions.Queries;

internal class GetTemplatePermissionsForUserByUserIdQueryValidator : AbstractValidator<GetTemplatePermissionsForUserByUserIdQuery>
{
    public GetTemplatePermissionsForUserByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithMessage("User ID is required");
        
        When(x => x.UserId != null, () =>
        {
            RuleFor(x => x.UserId)
                .Must(userId => userId!.Value != Guid.Empty)
                .WithMessage("User ID is required");
        });
    }
}

