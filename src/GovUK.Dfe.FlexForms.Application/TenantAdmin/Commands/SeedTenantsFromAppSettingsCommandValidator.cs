using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Commands;

internal class SeedTenantsFromAppSettingsCommandValidator : AbstractValidator<SeedTenantsFromAppSettingsCommand>
{
    public SeedTenantsFromAppSettingsCommandValidator()
    {
        // No fields to validate -- parameterless command. Validator exists for pipeline consistency.
    }
}
