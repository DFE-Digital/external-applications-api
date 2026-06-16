using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetApplicationsForUserQueryValidator : AbstractValidator<GetApplicationsForUserQuery>
{
    public GetApplicationsForUserQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Search)
            .SetValidator(new ApplicationListingSearchCriteriaValidator()!)
            .When(x => x.Search is not null);
    }
}