using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetApplicationByReferenceQueryValidator : AbstractValidator<GetApplicationByReferenceQuery>
{
    public GetApplicationByReferenceQueryValidator()
    {
        RuleFor(x => x.ApplicationReference)
            .NotEmpty()
            .WithMessage("Application reference is required")
            .MaximumLength(20)
            .WithMessage("Application reference cannot exceed 20 characters");
    }
} 