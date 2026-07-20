using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Commands;

internal class RefreshTenantConfigurationCommandValidator : AbstractValidator<RefreshTenantConfigurationCommand>
{
    public RefreshTenantConfigurationCommandValidator()
    {
        // No fields to validate -- parameterless command. Validator exists for pipeline consistency.
    }
}
