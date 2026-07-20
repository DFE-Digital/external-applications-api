using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.TenantConfig.Queries;

internal class GetTenantConfigurationQueryValidator : AbstractValidator<GetTenantConfigurationQuery>
{
    private static readonly string[] ValidTargets = ["Web", "Api", "Shared"];

    public GetTenantConfigurationQueryValidator()
    {
        RuleFor(x => x.Target)
            .NotEmpty()
            .WithMessage("Target is required.")
            .Must(t => ValidTargets.Contains(t))
            .WithMessage("Target must be one of: Web, Api, Shared.");
    }
}
