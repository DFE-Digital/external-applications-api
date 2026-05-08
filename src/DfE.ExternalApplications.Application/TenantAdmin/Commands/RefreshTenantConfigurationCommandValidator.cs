using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.TenantAdmin.Commands;

internal class RefreshTenantConfigurationCommandValidator : AbstractValidator<RefreshTenantConfigurationCommand>
{
    public RefreshTenantConfigurationCommandValidator()
    {
        // No fields to validate -- parameterless command. Validator exists for pipeline consistency.
    }
}
