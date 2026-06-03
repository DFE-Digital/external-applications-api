using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Resolves which applications a user can list based on their role and permission grants.
/// </summary>
public static class ApplicationAccessResolver
{
    /// <summary>
    /// Describes how application listing should be scoped for a user.
    /// </summary>
    public enum AccessMode
    {
        /// <summary>Only applications with explicit permission rows.</summary>
        SpecificApplicationIds,

        /// <summary>All applications in the tenant database.</summary>
        AllApplicationsInTenant,

        /// <summary>Applications belonging to templates the user can read.</summary>
        TemplateScoped
    }

    /// <summary>
    /// The resolved listing scope for a user.
    /// </summary>
    public sealed record AccessScope(
        AccessMode Mode,
        IReadOnlyCollection<ApplicationId> ApplicationIds,
        IReadOnlyCollection<TemplateId> TemplateIds)
    {
        public static AccessScope Empty { get; } = new(
            AccessMode.SpecificApplicationIds,
            Array.Empty<ApplicationId>(),
            Array.Empty<TemplateId>());
    }

    /// <summary>
    /// Resolves the application listing scope for the given user based on role.
    /// Standard users only see applications they have explicit permission rows for.
    /// The <c>Application:Any</c> read grant is used for authorization checks, not listing scope.
    /// </summary>
    public static AccessScope Resolve(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var roleName = user.Role?.Name;

        if (string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            return new AccessScope(AccessMode.AllApplicationsInTenant, Array.Empty<ApplicationId>(), Array.Empty<TemplateId>());

        if (string.Equals(roleName, RoleNames.Caseworker, StringComparison.OrdinalIgnoreCase))
            return ResolveCaseworkerScope(user);

        var applicationIds = user.Permissions
            .Where(p => p is { ApplicationId: not null, ResourceType: ResourceType.Application })
            .Select(p => p.ApplicationId!)
            .Distinct()
            .ToList();

        return new AccessScope(AccessMode.SpecificApplicationIds, applicationIds, Array.Empty<TemplateId>());
    }

    /// <summary>
    /// Returns true when the user may list all applications for the specified template
    /// (admin, or caseworker with template-scoped read access).
    /// </summary>
    public static bool CanListAllApplicationsForTemplate(User user, TemplateId templateId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(templateId);

        var scope = Resolve(user);
        return scope.Mode switch
        {
            AccessMode.AllApplicationsInTenant => true,
            AccessMode.TemplateScoped => scope.TemplateIds.Any(id => id.Value == templateId.Value),
            _ => false
        };
    }

    private static AccessScope ResolveCaseworkerScope(User user)
    {
        var templateIds = GetReadableTemplateIds(user);
        if (templateIds.Count > 0)
            return new AccessScope(AccessMode.TemplateScoped, Array.Empty<ApplicationId>(), templateIds);

        return AccessScope.Empty;
    }

    private static List<TemplateId> GetReadableTemplateIds(User user) =>
        user.TemplatePermissions
            .Where(tp => tp.AccessType == AccessType.Read)
            .Select(tp => tp.TemplateId)
            .Distinct()
            .ToList();
}
