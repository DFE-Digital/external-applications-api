using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class UserTemplateAccess : IEntity<UserTemplateAccessId>
{
    public UserTemplateAccessId? Id { get; private set; }
    public UserId UserId { get; private set; }
    public User? User { get; private set; }
    public TemplateId TemplateId { get; private set; }
    public Template? Template { get; private set; }
    public DateTime GrantedOn { get; private set; }
    public UserId GrantedBy { get; private set; }
    public User? GrantedByUser { get; private set; }

    private UserTemplateAccess() { /* For EF Core */ }

    /// <summary>
    /// Constructs a new UserTemplateAccess.
    /// </summary>
    public UserTemplateAccess(
        UserTemplateAccessId id,
        UserId userId,
        TemplateId templateId,
        DateTime grantedOn,
        UserId grantedBy)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        UserId = userId;
        TemplateId = templateId;
        GrantedOn = grantedOn;
        GrantedBy = grantedBy;
    }
}