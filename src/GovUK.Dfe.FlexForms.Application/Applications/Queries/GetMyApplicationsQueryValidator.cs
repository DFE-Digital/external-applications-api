using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

internal class GetMyApplicationsQueryValidator : AbstractValidator<GetMyApplicationsQuery>
{
    public GetMyApplicationsQueryValidator()
    {
        RuleFor(x => x.Search)
            .SetValidator(new ApplicationListingSearchCriteriaValidator()!)
            .When(x => x.Search is not null);
    }
}
