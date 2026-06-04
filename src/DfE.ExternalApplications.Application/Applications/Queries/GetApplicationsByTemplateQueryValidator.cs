using FluentValidation;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetApplicationsByTemplateQueryValidator : AbstractValidator<GetApplicationsByTemplateQuery>
{
    public GetApplicationsByTemplateQueryValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.Status)
            .IsEnumName(typeof(ApplicationStatus), false)
            .WithMessage("{PropertyName} '{PropertyValue}' is invalid");
    }
}
