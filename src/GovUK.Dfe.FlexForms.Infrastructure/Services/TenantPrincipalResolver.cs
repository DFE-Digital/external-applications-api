using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Infrastructure.Services;

/// <summary>
/// Resolves principals against the TenantPrincipals table in the tenant configuration database.
/// </summary>
public class TenantPrincipalResolver(TenantConfigDbContext dbContext) : ITenantPrincipalResolver
{
    /// <inheritdoc />
    public async Task<TenantPrincipalResolution?> ResolveAsync(
        string principalObjectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(principalObjectId))
            return null;

        var match = await dbContext.TenantPrincipals
            .AsNoTracking()
            .Where(p => p.PrincipalObjectId == principalObjectId && p.IsActive)
            .Join(
                dbContext.Tenants.AsNoTracking().Where(t => t.IsActive),
                p => p.TenantId,
                t => t.Id,
                (p, t) => new TenantPrincipalResolution(t.Id, t.Name, p.PrincipalType))
            .FirstOrDefaultAsync(cancellationToken);

        return match;
    }
}
