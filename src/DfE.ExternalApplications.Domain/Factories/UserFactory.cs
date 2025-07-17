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
        AddPermissionToUser(contributor, 
            applicationId.Value.ToString(), 
            ResourceType.Application, 
            new[] { AccessType.Read, AccessType.Write },
            createdBy, 
            applicationId, 
            when);

        // Raise domain event for contributor addition
        contributor.AddDomainEvent(new ContributorAddedEvent(
            applicationId,
            id,
            createdBy,
            when));

        return contributor;
    }

    public void AddPermissionToUser(
        User user,
        string resourceKey,
        ResourceType resourceType,
        AccessType[] accessTypes,
        UserId grantedBy,
        ApplicationId? applicationId = null,
        DateTime? grantedOn = null)
    {
        var when = grantedOn ?? DateTime.UtcNow;

        foreach (var accessType in accessTypes)
        {
            user.AddPermission(
                resourceKey,
                resourceType,
                accessType,
                grantedBy,
                applicationId,
                when);
        }
    }
}