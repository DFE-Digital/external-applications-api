using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;
public sealed class Role : BaseAggregateRoot, IEntity<RoleId>
{
    public RoleId? Id { get; private set; }
    public string Name { get; private set; } = null!;

    private Role() { }

    /// <summary>
    /// Constructs a new Role.
    /// </summary>
    public Role(RoleId id, string name)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
    }
}
