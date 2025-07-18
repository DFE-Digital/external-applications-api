//using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.Applications.EventHandlers;
//using DfE.ExternalApplications.Domain.Entities;
//using DfE.ExternalApplications.Domain.Events;
//using DfE.ExternalApplications.Domain.Interfaces.Repositories;
//using DfE.ExternalApplications.Domain.ValueObjects;
//using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
//using Microsoft.Extensions.Logging;
//using NSubstitute;
//using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

//namespace DfE.ExternalApplications.Application.Tests.EventHandlers;

//public class ContributorAddedEventHandlerTests
//{
//    [Theory]
//    [CustomAutoData(typeof(ApplicationCustomization))]
//    public async Task Handle_ShouldCreateReadAndWritePermissions_WhenEventReceived(
//        ApplicationId applicationId,
//        UserId contributorId,
//        UserId addedBy,
//        DateTime addedOn,
//        ILogger<ContributorAddedEventHandler> logger,
//        IEaRepository<Permission> permissionRepo)
//    {
//        // Arrange
//        var @event = new ContributorAddedEvent(applicationId, contributorId, addedBy, addedOn);
//        var handler = new ContributorAddedEventHandler(logger, permissionRepo);

//        // Act
//        await handler.Handle(@event, CancellationToken.None);

//        // Assert
//        await permissionRepo.Received(2).AddAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
        
//        // Verify that both read and write permissions were created
//        await permissionRepo.Received(1).AddAsync(
//            Arg.Is<Permission>(p => 
//                p.UserId == contributorId && 
//                p.ApplicationId == applicationId && 
//                p.ResourceType == ResourceType.Application && 
//                p.AccessType == AccessType.Read), 
//            Arg.Any<CancellationToken>());
            
//        await permissionRepo.Received(1).AddAsync(
//            Arg.Is<Permission>(p => 
//                p.UserId == contributorId && 
//                p.ApplicationId == applicationId && 
//                p.ResourceType == ResourceType.Application && 
//                p.AccessType == AccessType.Write), 
//            Arg.Any<CancellationToken>());
//    }
//} 