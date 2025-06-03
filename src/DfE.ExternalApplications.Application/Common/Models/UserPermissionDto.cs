using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Common.Models
{
    public sealed class UserPermissionDto
    {
        public Guid ApplicationId { get; init; }
        public string ResourceKey { get; init; } = string.Empty;
        public AccessType AccessType { get; init; }

    }
}