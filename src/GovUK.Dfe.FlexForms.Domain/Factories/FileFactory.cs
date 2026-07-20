using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Events;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Domain.Factories;

public class FileFactory : IFileFactory
{
    public File CreateUpload(
        FileId id,
        Application application,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy,
        long fileSize,
        string fileHash)
    {
        ArgumentNullException.ThrowIfNull(application);

        var file = new File(
            id,
            application.Id!,
            name,
            description,
            originalFileName,
            fileName,
            path,
            uploadedOn,
            uploadedBy,
            fileSize
        );

        // Set the navigation property so domain events have access to the Application
        file.SetApplication(application);

        file.AddDomainEvent(new FileUploadedDomainEvent(file, fileHash, uploadedOn));

        return file;
    }

    public void DeleteFile(File file)
    {
        file.Delete();

        var when = DateTime.UtcNow;

        file.AddDomainEvent(new FileDeletedEvent(file.Id!, when));
    }
} 
