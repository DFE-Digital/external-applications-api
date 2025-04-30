using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Domain.ValueObjects
{
    public record SchoolId(int Value) : IStronglyTypedId;

}
