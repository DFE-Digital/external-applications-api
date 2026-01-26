using System;
using System.Collections.Generic;
using System.IO;
using DfE.ExternalApplications.Application.Common.EventHandlers;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;
using Microsoft.Extensions.Logging;
using DfE.ExternalApplications.Domain.Tenancy;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class FileUploadedDomainEventHandler(
    ILogger<FileUploadedDomainEventHandler> logger,
    IEventPublisher publishEndpoint,
    ITenantContextAccessor tenantContextAccessor,
    IAzureSpecificOperations azureSpecificOperations)
    : BaseEventHandler<Domain.Events.FileUploadedDomainEvent>(logger)
{
    protected override async Task HandleEvent(
        Domain.Events.FileUploadedDomainEvent notification, 
        CancellationToken cancellationToken)
    {
        var file = notification.File;

        var fileName = file.FileName;

        // FileURL is the Azure File Share path: {applicationReference}/{hashedFileName}
        var fileUrl = $"{file.Path}/{fileName}";

        string sasUri;

        // Check if the service is running in a local environment
        if (InstanceIdentifierHelper.IsLocalEnvironment())
        {
            // Build fake file:// URI so local function can load from disk
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileUrl);
            sasUri = $"file:///{localPath.Replace("\\", "/")}";
            logger.LogInformation("Local environment detected - using fake SAS URI: {SasUri}", sasUri);
        }
        else
        {
            // Real Azure File Share SAS
            sasUri = await azureSpecificOperations.GenerateSasTokenAsync(
                fileUrl, DateTimeOffset.UtcNow.AddHours(1), "r", cancellationToken);
        }

        var tenant = tenantContextAccessor.CurrentTenant 
            ?? throw new InvalidOperationException("Tenant context is required to publish file upload events.");

        // Create the integration event
        var fileUploadedEvent = new GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events.ScanRequestedEvent(
            FileId:file.Id?.Value.ToString(),
            FileHash: notification.FileHash,
            Reference:file.ApplicationId.Value.ToString(),
            FileName: fileName,
            Path:file.Path,
            IsAzureFileShare: true,
            FileUri: sasUri,
            ServiceName: $"extapi-{tenant.Name}",
            Metadata: new Dictionary<string, object>
            {
                { "TenantId", tenant.Id.ToString() },
                { "TenantName", tenant.Name },
                { "Reference", file.Application?.ApplicationReference! },
                { "applicationId", file.ApplicationId.Value },
                { "userId", file.UploadedBy.Value },
                { "originalFileName", file.OriginalFileName },
                { "InstanceIdentifier", InstanceIdentifierHelper.GetInstanceIdentifier(tenant.Settings) ?? "" },
            }
        );

        // Build Azure Service Bus message properties
        var messageProperties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty("serviceName", $"extapi-{tenant.Name}")
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
