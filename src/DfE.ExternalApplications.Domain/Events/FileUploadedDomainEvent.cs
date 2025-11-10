using DfE.ExternalApplications.Domain.Common;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Domain.Events;

public sealed record FileUploadedDomainEvent(
    File File,
    string FileHash,
    DateTime UploadedOn) : IDomainEvent
{
    public DateTime OccurredOn => UploadedOn;
}

