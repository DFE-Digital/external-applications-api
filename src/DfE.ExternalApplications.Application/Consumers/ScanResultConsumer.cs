using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Consumers;

/// <summary>
/// Consumer for file scan results from the virus scanner service.
/// Listens to the file-scanner-results topic with subscription extapi.
/// </summary>
public sealed class ScanResultConsumer(
    ILogger<ScanResultConsumer> logger,
    IEaRepository<File> fileRepository,
    ISender sender) : IConsumer<ScanResultEvent>
{
    public async Task Consume(ConsumeContext<ScanResultEvent> context)
    {
        var scanResult = context.Message;
        
        logger.LogInformation(
            "Received scan result - FileName: {FileName}, Status: {Status}, Outcome: {Outcome}",
            scanResult.FileName,
            scanResult.Status,
            scanResult.Outcome);

        try
        {
            // Parse the FileUrl to extract path and filename
            // FileUrl format: "{applicationReference}/{hashedFileName}"
            if (string.IsNullOrEmpty(scanResult.FileName) || string.IsNullOrEmpty(scanResult.Path))
            {
                logger.LogWarning("ScanResultEvent has no FileUri, skipping");
                return;
            }

            var path = scanResult.Path; // Application reference
            var fileName = scanResult.FileName; // Hashed file name

            var file = await new GetFileByPathAndFileNameQueryObject(path, fileName)
                .Apply(fileRepository.Query().Include(f => f.UploadedByUser).AsNoTracking())
                .FirstOrDefaultAsync(context.CancellationToken);

            if (file == null)
            {
                logger.LogWarning(
                    "File not found in database - Path: {Path}, FileName: {FileName}",
                    path,
                    fileName);
                return;
            }

            // Handle based on scan outcome
            switch (scanResult.Outcome)
            {
                case VirusScanOutcome.Clean:
                    logger.LogInformation(
                        "File is clean - FileId: {FileId}, FileName: {FileName}",
                        file.Id!.Value,
                        scanResult.FileName);
                    break;

                case VirusScanOutcome.Infected:
                    logger.LogWarning(
                        "File is infected - FileId: {FileId}, FileName: {FileName}, Malware: {MalwareName}",
                        file.Id!.Value,
                        scanResult.FileName,
                        scanResult.MalwareName);

                    // Delete the infected file
                    var deleteCommand = new DeleteInfectedFileCommand(file.Id);
                    var result = await sender.Send(deleteCommand, context.CancellationToken);

                    if (result.IsSuccess)
                    {
                        logger.LogWarning(
                            "Successfully deleted infected file - FileId: {FileId}",
                            file.Id.Value);
                    }
                    else
                    {
                        logger.LogError(
                            "Failed to delete infected file - FileId: {FileId}, Error: {Error}",
                            file.Id.Value,
                            result.Error);
                    }
                    break;

                case VirusScanOutcome.Error:
                    logger.LogWarning(
                        "File scan result unknown - FileId: {FileId}, FileName: {FileName}, Message: {Message}",
                        file.Id!.Value,
                        scanResult.FileName,
                        scanResult.Message);
                    break;

                default:
                    logger.LogInformation(
                        "File scan status: {Status} - FileId: {FileId}, FileName: {FileName}",
                        scanResult.Status,
                        file.Id!.Value,
                        scanResult.FileName);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error processing scan result for file: {FileName}",
                scanResult.FileName);
            throw; // Let MassTransit handle retries
        }
    }
}