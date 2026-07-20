using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

public class GenerateApplicationPreviewHtmlQueryValidator : AbstractValidator<GenerateApplicationPreviewHtmlQuery>
{
    public GenerateApplicationPreviewHtmlQueryValidator()
    {
        RuleFor(x => x.ApplicationReference)
            .NotEmpty()
            .WithMessage("Application reference is required");
    }
}
