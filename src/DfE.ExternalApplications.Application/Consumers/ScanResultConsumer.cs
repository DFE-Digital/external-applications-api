using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Exceptions;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    IConfiguration configuration,
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

        // LOCAL ENVIRONMENT ONLY: Check if this message is for this instance
        // This allows developers to run locally without interfering with each other
        if (InstanceIdentifierHelper.IsLocalEnvironment())
        {
            var messageInstanceId = scanResult.Metadata?.ContainsKey("InstanceIdentifier") == true
                ? scanResult.Metadata["InstanceIdentifier"]?.ToString()
                : null;

            var localInstanceId = InstanceIdentifierHelper.GetInstanceIdentifier(configuration);

            if (!InstanceIdentifierHelper.IsMessageForThisInstance(messageInstanceId, localInstanceId))
            {
                logger.LogDebug(
                    "Message {FileId} not for this instance (MessageInstanceId: '{MessageInstanceId}', LocalInstanceId: '{LocalInstanceId}') - throwing exception to requeue for other consumers",
                    scanResult.FileId,
                    messageInstanceId ?? "none",
                    localInstanceId ?? "none");

                // Throw exception to prevent acknowledgment and allow other consumers to process
                // Service Bus will redeliver this message to another consumer instance
                throw new MessageNotForThisInstanceException(
                    $"Message InstanceIdentifier '{messageInstanceId}' doesn't match local instance '{localInstanceId}'");
            }
        }

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