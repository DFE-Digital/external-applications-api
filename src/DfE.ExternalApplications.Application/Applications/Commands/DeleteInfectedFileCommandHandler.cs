using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.Commands;

/// <summary>
/// Internal handler to delete infected files without authentication.
/// Used by background consumers for automated file deletion.
/// </summary>
internal sealed class DeleteInfectedFileCommandHandler(
    IEaRepository<File> fileRepository,
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService,
    IFileFactory fileFactory,
    ILogger<DeleteInfectedFileCommandHandler> logger)
    : IRequestHandler<DeleteInfectedFileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteInfectedFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the file by ID
            var file = await new GetFileByIdQueryObject(request.FileId)
                .Apply(fileRepository.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (file == null)
            {
                logger.LogWarning("Infected file not found in database: {FileId}", request.FileId);
                return Result<bool>.NotFound("File not found");
            }

            // Construct storage path
            var storagePath = $"{file.Path}/{file.FileName}";
            
            logger.LogWarning(
                "Deleting infected file - FileId: {FileId}, Name: {FileName}, Path: {StoragePath}",
                file.Id!.Value,
                file.OriginalFileName,
                storagePath);

            // Delete from storage
            try
            {
                await fileStorageService.DeleteAsync(storagePath, cancellationToken);
                logger.LogInformation("Successfully deleted infected file from storage: {StoragePath}", storagePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete infected file from storage: {StoragePath}", storagePath);
                // Continue with database deletion even if storage deletion fails
            }

            // Mark as deleted and raise domain event
            fileFactory.DeleteFile(file);

            // Remove from database
            await fileRepository.RemoveAsync(file, cancellationToken);
            
            // Commit changes
            await unitOfWork.CommitAsync(cancellationToken);

            logger.LogWarning("Successfully deleted infected file from database: {FileId}", file.Id.Value);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting infected file: {FileId}", request.FileId);
            return Result<bool>.Failure(ex.Message);
        }
    }
}

