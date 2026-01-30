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
    public long FileSize { get; private set; }
    public string Path { get; private set; }
    public DateTime UploadedOn { get; private set; }
    public UserId UploadedBy { get; private set; }
    public User? UploadedByUser { get; private set; }
    public bool IsDeleted { get; private set; }

    private File() { /* For EF Core */ }

    public File(
        FileId id,
        ApplicationId applicationId,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy,
        long fileSize)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ApplicationId = applicationId ?? throw new ArgumentNullException(nameof(applicationId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Path = path;
        UploadedOn = uploadedOn;
        UploadedBy = uploadedBy ?? throw new ArgumentNullException(nameof(uploadedBy));
        FileSize = fileSize;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("File is already deleted.");
        IsDeleted = true;
    }

    public void SetApplication(Application application)
    {
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        if (application.Id != ApplicationId)
            throw new InvalidOperationException("Application Id must match the File's ApplicationId");

        Application = application;
    }
} 