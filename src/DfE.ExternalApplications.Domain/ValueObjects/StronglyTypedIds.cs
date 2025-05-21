using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Domain.ValueObjects
{
    public record RoleId(Guid Value) : IStronglyTypedId;
    public record UserId(Guid Value) : IStronglyTypedId;
    public record TemplateId(Guid Value) : IStronglyTypedId;
    public record TemplateVersionId(Guid Value) : IStronglyTypedId;
    public record ApplicationId(Guid Value) : IStronglyTypedId;
    public record ResponseId(Guid Value) : IStronglyTypedId;
    public record PermissionId(Guid Value) : IStronglyTypedId;
    public record UserTemplateAccessId(Guid Value) : IStronglyTypedId;
    public record TaskAssignmentLabelId(Guid Value) : IStronglyTypedId;
}
