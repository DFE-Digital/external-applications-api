using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class User : BaseAggregateRoot, IEntity<UserId>
{
    public UserId? Id { get; private set; }
    public RoleId RoleId { get; private set; }
    public Role? Role { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedOn { get; private set; }
    public UserId? CreatedBy { get; private set; }
    public User? CreatedByUser { get; private set; }
    public DateTime? LastModifiedOn { get; private set; }
    public UserId? LastModifiedBy { get; private set; }
    public User? LastModifiedByUser { get; private set; }
    public string? ExternalProviderId { get; private set; }

    private readonly List<Permission> _permissions = new();
    private readonly List<TemplatePermission> _templatePermissions = new();

    public IReadOnlyCollection<Permission> Permissions
        => _permissions.AsReadOnly();

    public IReadOnlyCollection<TemplatePermission> TemplatePermissions
        => _templatePermissions.AsReadOnly();

    private User()
    {
        // Required by EF Core to materialise the entity.
    }

    /// <summary>
    /// Constructs a new User with all required fields. 
    /// Pass in null for optional fields (CreatedBy, LastModifiedOn, LastModifiedBy).
    /// </summary>
    public User(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        DateTime createdOn,
        UserId? createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy,
        IEnumerable<Permission>? initialPermissions = null,
        IEnumerable<TemplatePermission>? initialTemplatePermissions = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        RoleId = roleId ?? throw new ArgumentNullException(nameof(roleId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = (email ?? throw new ArgumentNullException(nameof(email))).Trim();
        CreatedOn = createdOn;
        CreatedBy = createdBy;
        LastModifiedOn = lastModifiedOn;
        LastModifiedBy = lastModifiedBy;

        if (initialPermissions != null)
        {
            _permissions.AddRange(initialPermissions);
        }

        if (initialTemplatePermissions != null)
        {
            _templatePermissions.AddRange(initialTemplatePermissions);
        }
    }

    /// <summary>
    /// Factory method to create and attach a new Permission to this User.
    /// </summary>
    public Permission AddPermission(
        ApplicationId applicationId,
        string resourceKey,
        ResourceType resourceType,
        AccessType accessType,
        UserId grantedBy,
        DateTime? grantedOn = null)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new ArgumentException("ResourceKey cannot be empty", nameof(resourceKey));

        var id = new PermissionId(Guid.NewGuid());
        var when = grantedOn ?? DateTime.UtcNow;

        var permission = new Permission(
            id,
            this.Id ?? throw new InvalidOperationException("UserId must be set before adding a permission."),
            applicationId,
            resourceKey,
            resourceType,
            accessType,
            when,
            grantedBy);

        _permissions.Add(permission);
        return permission;
    }
}