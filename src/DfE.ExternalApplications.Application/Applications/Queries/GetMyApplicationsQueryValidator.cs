using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetMyApplicationsQueryValidator : AbstractValidator<GetMyApplicationsQuery>
{
    public GetMyApplicationsQueryValidator()
    {
        RuleFor(x => x.Search)
            .SetValidator(new ApplicationListingSearchCriteriaValidator()!)
            .When(x => x.Search is not null);
    }
}
