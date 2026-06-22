using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetApplicationsForUserByExternalProviderIdQueryValidator : AbstractValidator<GetApplicationsForUserByExternalProviderIdQuery>
{
    public GetApplicationsForUserByExternalProviderIdQueryValidator()
    {
        RuleFor(x => x.ExternalProviderId)
            .NotEmpty()
            .WithMessage("External Provider ID is required");

        RuleFor(x => x.Search)
            .SetValidator(new ApplicationListingSearchCriteriaValidator()!)
            .When(x => x.Search is not null);
    }
}

