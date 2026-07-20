using GovUK.Dfe.FlexForms.Domain.Common;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Domain.Events;

public sealed record FileUploadedDomainEvent(
    File File,
    string FileHash,
    DateTime UploadedOn) : IDomainEvent
{
    public DateTime OccurredOn => UploadedOn;
}

