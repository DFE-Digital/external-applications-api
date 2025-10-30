using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using GovUK.Dfe.CoreLibs.Notifications.Models;
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
    IEaRepository<User> userRepository,
    INotificationService notificationService,
    INotificationSignalRService notificationSignalRService,
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

            // Find the file in the database (include user for notifications)
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

                        // Notify the user about the infected file deletion
                        await NotifyUserAboutInfectedFileAsync(file, scanResult, context.CancellationToken);
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

    private async Task NotifyUserAboutInfectedFileAsync(
        File file,
        ScanResultEvent scanResult,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the user who uploaded the file
            var user = file.UploadedByUser;
            if (user == null)
            {
                // Try to load user if not included
                user = await new GetUserByIdQueryObject(file.UploadedBy)
                    .Apply(userRepository.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (user == null)
            {
                logger.LogWarning(
                    "Cannot send notification - user not found for FileId: {FileId}, UserId: {UserId}",
                    file.Id!.Value,
                    file.UploadedBy.Value);
                return;
            }

            // Create notification message
            var malwareInfo = !string.IsNullOrEmpty(scanResult.MalwareName) 
                ? $" ({scanResult.MalwareName})" 
                : string.Empty;
            
            var message = $"Your file '{file.OriginalFileName}' was found to be infected{malwareInfo} and has been automatically deleted for security reasons.";

            var notificationOptions = new NotificationOptions
            {
                Category = "Security",
                Context = $"file-scan-{file.Id!.Value}",
                AutoDismiss = false, // Don't auto-dismiss security warnings
                AutoDismissSeconds = 0,
                UserId = user.Email,
                Priority = NotificationPriority.High,
                ReplaceExistingContext = true,
                Metadata = new Dictionary<string, object>
                {
                    { "fileId", file.Id.Value },
                    { "fileName", file.OriginalFileName },
                    { "applicationId", file.ApplicationId.Value },
                    { "malwareName", scanResult.MalwareName ?? "Unknown" },
                    { "scannedAt", scanResult.ScannedAt?.ToString("o") ?? DateTime.UtcNow.ToString("o") }
                }
            };

            // Save notification to database
            var notification = await notificationService.AddNotificationAsync(
                message,
                NotificationType.Warning,
                notificationOptions,
                cancellationToken);

            // Create DTO for SignalR
            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                Category = notification.Category,
                Context = notification.Context,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                AutoDismiss = notification.AutoDismiss,
                AutoDismissSeconds = notification.AutoDismissSeconds,
                UserId = notification.UserId,
                ActionUrl = notification.ActionUrl,
                Metadata = notification.Metadata,
                Priority = notification.Priority
            };

            // Send real-time notification via SignalR
            await notificationSignalRService.SendNotificationToUserAsync(
                user.Email,
                notificationDto,
                cancellationToken);

            logger.LogInformation(
                "Sent infected file notification to user: {Email} for file: {FileName}",
                user.Email,
                file.OriginalFileName);
        }
        catch (Exception ex)
        {
            // Don't throw - notification failure shouldn't fail the entire scan result processing
            logger.LogError(
                ex,
                "Failed to send notification for infected file - FileId: {FileId}",
                file.Id!.Value);
        }
    }
}