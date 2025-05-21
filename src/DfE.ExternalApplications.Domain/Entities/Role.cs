using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;
public sealed class Role : BaseAggregateRoot, IEntity<RoleId>
{
    public RoleId? Id { get; private set; }
    public string Name { get; private set; } = null!;
}
