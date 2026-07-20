using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Application.Common.EventHandlers;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Events;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Application.Applications.EventHandlers;

public sealed class ApplicationCreatedEventHandler(
    ILogger<ApplicationCreatedEventHandler> logger,
    IEaRepository<Permission> permissionRepo) : BaseEventHandler<ApplicationCreatedEvent>(logger)
{
    protected override async Task HandleEvent(ApplicationCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Create read and write permissions for the creator
        var readPermission = new Permission(
            new PermissionId(Guid.NewGuid()),
            notification.CreatedBy,
            notification.ApplicationId,
            notification.ApplicationId.Value.ToString(),
            ResourceType.Application,
            AccessType.Read,
            notification.CreatedOn,
            notification.CreatedBy);

        var writePermission = new Permission(
            new PermissionId(Guid.NewGuid()),
            notification.CreatedBy,
            notification.ApplicationId,
            notification.ApplicationId.Value.ToString(),
            ResourceType.Application,
            AccessType.Write,
            notification.CreatedOn,
            notification.CreatedBy);

        var filesWritePermission = new Permission(
            new PermissionId(Guid.NewGuid()),
            notification.CreatedBy,
            notification.ApplicationId,
            notification.ApplicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            AccessType.Write,
            notification.CreatedOn,
            notification.CreatedBy);

        var filesReadPermission = new Permission(
            new PermissionId(Guid.NewGuid()),
            notification.CreatedBy,
            notification.ApplicationId,
            notification.ApplicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            AccessType.Read,
            notification.CreatedOn,
            notification.CreatedBy);

        var filesDeletePermission = new Permission(
            new PermissionId(Guid.NewGuid()),
            notification.CreatedBy,
            notification.ApplicationId,
            notification.ApplicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            AccessType.Delete,
            notification.CreatedOn,
            notification.CreatedBy);

        await permissionRepo.AddAsync(readPermission, cancellationToken);
        await permissionRepo.AddAsync(writePermission, cancellationToken);
        await permissionRepo.AddAsync(filesReadPermission, cancellationToken);
        await permissionRepo.AddAsync(filesWritePermission, cancellationToken);
        await permissionRepo.AddAsync(filesDeletePermission, cancellationToken);
    }
} 
