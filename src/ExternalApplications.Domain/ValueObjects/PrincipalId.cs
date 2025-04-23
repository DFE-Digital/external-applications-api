using ExternalApplications.Domain.Common;

namespace ExternalApplications.Domain.ValueObjects
{
    public record PrincipalId(int Value) : IStronglyTypedId;
}
