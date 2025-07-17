using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using System.Security;
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
        if (id == null)
            throw new ArgumentException("Id cannot be null", nameof(id));
        
        if (roleId == null)
            throw new ArgumentException("RoleId cannot be null", nameof(roleId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        
        if (createdBy == null)
            throw new ArgumentException("CreatedBy cannot be null", nameof(createdBy));
        
        if (applicationId == null)
            throw new ArgumentException("ApplicationId cannot be null", nameof(applicationId));

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
        if (user == null)
            throw new ArgumentException("User cannot be null", nameof(user));
        
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new ArgumentException("ResourceKey cannot be null or empty", nameof(resourceKey));
        
        if (accessTypes == null)
            throw new ArgumentException("AccessTypes cannot be null", nameof(accessTypes));
        
        if (grantedBy == null)
            throw new ArgumentException("GrantedBy cannot be null", nameof(grantedBy));

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

    public bool RemovePermissionFromUser(
        User user,
        Permission permission)
    {
        if (user == null)
            throw new ArgumentException("User cannot be null", nameof(user));
        
        if (permission == null)
            throw new ArgumentException("Permission cannot be null", nameof(permission));

        return user.RemovePermission(permission);
    }
}