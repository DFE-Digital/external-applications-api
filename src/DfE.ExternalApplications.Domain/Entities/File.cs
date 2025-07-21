using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using System;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class File : BaseAggregateRoot, IEntity<FileId>
{
    public FileId? Id { get; private set; }
    public ApplicationId ApplicationId { get; private set; }
    public Application? Application { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public DateTime UploadedOn { get; private set; }
    public UserId UploadedBy { get; private set; }
    public User? UploadedByUser { get; private set; }

    private File() { /* For EF Core */ }

    public File(
        FileId id,
        ApplicationId applicationId,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ApplicationId = applicationId ?? throw new ArgumentNullException(nameof(applicationId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        UploadedOn = uploadedOn;
        UploadedBy = uploadedBy ?? throw new ArgumentNullException(nameof(uploadedBy));
    }
} 