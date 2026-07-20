using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Templates.Queries;

internal class GetLatestTemplateSchemaByUserIdQueryValidator : AbstractValidator<GetLatestTemplateSchemaByUserIdQuery>
{
    public GetLatestTemplateSchemaByUserIdQueryValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

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

