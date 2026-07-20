using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

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

