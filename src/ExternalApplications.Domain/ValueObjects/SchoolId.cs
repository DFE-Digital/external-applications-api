using ExternalApplications.Domain.Common;

namespace ExternalApplications.Domain.ValueObjects
{
    public record SchoolId(int Value) : IStronglyTypedId;

}
