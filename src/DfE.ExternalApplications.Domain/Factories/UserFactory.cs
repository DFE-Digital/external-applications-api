using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public class UserFactory : IUserFactory
{
    public User CreateContributor(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        UserId createdBy,
        ApplicationId applicationId,
        DateTime? createdOn = null)
    {
        var when = createdOn ?? DateTime.UtcNow;
        
        var contributor = new User(
            id,
            roleId,
            name,
            email,
            when,
            createdBy,
            null,
            null);

        // Add Read and Write permissions to access the application
        contributor.AddPermission(
            applicationId,
            applicationId.Value.ToString(),
            ResourceType.Application,
            AccessType.Read,
            createdBy,
            when);

        contributor.AddPermission(
            applicationId,
            applicationId.Value.ToString(),
            ResourceType.Application,
            AccessType.Write,
            createdBy,
            when);

        // Raise domain event for contributor addition
        contributor.AddDomainEvent(new ContributorAddedEvent(
            applicationId,
            id,
            createdBy,
            when));

        return contributor;
    }
} 