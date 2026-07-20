using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

internal class GetApplicationsByTemplateQueryValidator : AbstractValidator<GetApplicationsByTemplateQuery>
{
    public GetApplicationsByTemplateQueryValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.Search)
            .SetValidator(new ApplicationListingSearchCriteriaValidator()!)
            .When(x => x.Search is not null);
    }
}
