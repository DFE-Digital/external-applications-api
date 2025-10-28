using DfE.ExternalApplications.Application.Common.EventHandlers;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class FileUploadedDomainEventHandler(
    ILogger<FileUploadedDomainEventHandler> logger,
    IEventPublisher publishEndpoint,
    IConfiguration configuration,
    IAzureSpecificOperations azureSpecificOperations)
    : BaseEventHandler<Domain.Events.FileUploadedDomainEvent>(logger)
{
    protected override async Task HandleEvent(
        Domain.Events.FileUploadedDomainEvent notification, 
        CancellationToken cancellationToken)
    {
        var file = notification.File;

        var fileName = "db6e818ed7721c8fc660a6ca43544d8b.jpg";

        // FileURL is the Azure File Share path: {applicationReference}/{hashedFileName}
        var fileUrl = $"{file.Path}/{fileName}";

        var sasUri = await azureSpecificOperations.GenerateSasTokenAsync(fileUrl, DateTimeOffset.UtcNow.AddMinutes(10), "r", cancellationToken);

        // Create the integration event
        var fileUploadedEvent = new GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events.ScanRequestedEvent(
            FileId:file.Id?.Value.ToString(),
            FileHash: notification.FileHash,
            Reference:file.Path,
            FileName: fileName,
            Path:file.Path,
            IsAzureFileShare: true,
            FileUri: sasUri,
            ServiceName: "extapi"
        );

        // Build Azure Service Bus message properties
        var messageProperties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty("serviceName", "extapi")
            .Build();

        // Publish to Azure Service Bus via MassTransit
        await publishEndpoint.PublishAsync(
            fileUploadedEvent, 
            messageProperties, 
            cancellationToken);

        logger.LogInformation(
            "Published ScanRequestedEvent to service bus - File: {FileName}",
            file.OriginalFileName);
    }
}

