using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Templates.Queries
{
    internal class GetLatestTemplateSchemaQueryValidator : AbstractValidator<GetLatestTemplateSchemaQuery>
    {
        public GetLatestTemplateSchemaQueryValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty();
        }
    }
}
