using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

/// <summary>
/// Validates application listing search criteria.
/// </summary>
internal class ApplicationListingSearchCriteriaValidator : AbstractValidator<ApplicationListingSearchCriteria>
{
    public ApplicationListingSearchCriteriaValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.DateStartedFrom.HasValue || !x.DateStartedTo.HasValue || x.DateStartedFrom <= x.DateStartedTo)
            .WithMessage("DateStartedFrom must be on or before DateStartedTo");

        RuleFor(x => x)
            .Must(x => !x.DateSubmittedFrom.HasValue || !x.DateSubmittedTo.HasValue || x.DateSubmittedFrom <= x.DateSubmittedTo)
            .WithMessage("DateSubmittedFrom must be on or before DateSubmittedTo");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}
