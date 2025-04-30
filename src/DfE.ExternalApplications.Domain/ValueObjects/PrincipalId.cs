using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Domain.ValueObjects
{
    public record PrincipalId(int Value) : IStronglyTypedId;
}
