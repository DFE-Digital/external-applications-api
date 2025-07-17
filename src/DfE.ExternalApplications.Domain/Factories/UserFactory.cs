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

        // Raise domain event for contributor addition (permissions will be added in the event handler)
        contributor.AddDomainEvent(new ContributorAddedEvent(
            applicationId,
            contributor,
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