using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Queries;

internal class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        // No fields to validate -- parameterless query. Validator exists for pipeline consistency.
    }
}
