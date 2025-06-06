using FluentValidation;

namespace DfE.ExternalApplications.Application.Templates.Queries
{
    internal class GetLatestTemplateSchemaQueryValidator : AbstractValidator<GetLatestTemplateSchemaQuery>
    {
        public GetLatestTemplateSchemaQueryValidator()
        {
            RuleFor(x => x.TemplateName)
                .NotEmpty();
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}
