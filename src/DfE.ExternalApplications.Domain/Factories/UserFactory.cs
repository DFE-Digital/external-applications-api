using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
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
        string applicationReference,
        TemplateId templateId,
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

        if (string.IsNullOrWhiteSpace(applicationReference))
            throw new ArgumentException("ApplicationReference cannot be null or empty", nameof(applicationReference));

        if (templateId == null)
            throw new ArgumentException("TemplateId cannot be null", nameof(templateId));

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

        // Add all required permissions directly (idempotent)
        
        // Application permissions
        AddPermissionToUser(
            contributor,
            applicationId.Value.ToString(),
            ResourceType.Application,
            new[] { AccessType.Read, AccessType.Write },
            createdBy,
            applicationId,
            when);

        // Application files permissions
        AddPermissionToUser(
            contributor,
            applicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            new[] { AccessType.Read, AccessType.Write, AccessType.Delete },
            createdBy,
            applicationId,
            when);

        // Notifications permissions
        AddPermissionToUser(
            contributor,
            email,
            ResourceType.Notifications,
            new[] { AccessType.Read, AccessType.Write, AccessType.Delete },
            createdBy,
            applicationId,
            when);

        // Template permissions
        AddTemplatePermissionToUser(
            contributor,
            templateId.Value.ToString(),
            new[] { AccessType.Read, AccessType.Write },
            createdBy,
            when);

        // Raise domain event for contributor addition (side effects like email)
        contributor.AddDomainEvent(new ContributorAddedEvent(
            applicationId,
            applicationReference,
            templateId,
            contributor,
            createdBy,
            when));

        return contributor;
    }

    public User CreateUser(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        TemplateId templateId ,
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

        var when = createdOn ?? DateTime.UtcNow;
        
        var user = new User(
            id,
            roleId,
            name,
            email,
            when,
            null, // CreatedBy is null for self-registered users
            null,
            null);

        // Add self permission to the user to read their own user record
        AddPermissionToUser(
            user,
            email,
            ResourceType.User,
            new[] { AccessType.Read },
            id, // User grants permission to themselves
            null, // No application context
            when);

        // Add notification permissions for the user's own email
        AddPermissionToUser(
            user,
            email,
            ResourceType.Notifications,
            new[] { AccessType.Read, AccessType.Write, AccessType.Delete },
            id, // User grants permission to themselves
            null, // No application context
            when);

        // Add template permissions if TemplateId is provided
        AddTemplatePermissionToUser(
            user,
            templateId.Value.ToString(),
            new[] { AccessType.Read, AccessType.Write },
            id, // User grants permission to themselves
            when);

        // Add application permissions for "Any" application
        AddPermissionToUser(
            user,
            "Any",
            ResourceType.Application,
            new[] { AccessType.Read, AccessType.Write },
            id,
            null,
            when);

        // Raise domain event for user creation (side effects like email)
        user.AddDomainEvent(new UserCreatedEvent(
            user,
            when));

        return user;
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
            // Check if permission already exists (idempotent)
            var hasPermission = user.Permissions
                .Any(p => p.ResourceType == resourceType && 
                         p.ResourceKey == resourceKey && 
                         p.AccessType == accessType &&
                         (applicationId == null || p.ApplicationId == applicationId));

            if (!hasPermission)
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

    public void AddTemplatePermissionToUser(
        User user,
        string templateId,
        AccessType[] accessTypes,
        UserId grantedBy,
        DateTime? grantedOn = null)
    {
        if (user == null)
            throw new ArgumentException("User cannot be null", nameof(user));
        
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("TemplateId cannot be null or empty", nameof(templateId));
        
        if (accessTypes == null)
            throw new ArgumentException("AccessTypes cannot be null", nameof(accessTypes));
        
        if (grantedBy == null)
            throw new ArgumentException("GrantedBy cannot be null", nameof(grantedBy));

        var when = grantedOn ?? DateTime.UtcNow;

        foreach (var accessType in accessTypes)
        {
            // Check if template permission already exists (idempotent)
            var hasTemplatePermission = user.TemplatePermissions
                .Any(tp => tp.TemplateId.Value.ToString() == templateId && tp.AccessType == accessType);

            if (!hasTemplatePermission)
            {
                user.AddTemplatePermission(
                    templateId,
                    accessType,
                    grantedBy,
                    when);
            }
        }
    }

    /// <inheritdoc />
    public void EnsureUserHasTemplatePermission(
        User user,
        TemplateId templateId,
        UserId grantedBy,
        DateTime? grantedOn = null)
    {
        if (user == null)
            throw new ArgumentException("User cannot be null", nameof(user));
        if (templateId == null)
            throw new ArgumentException("TemplateId cannot be null", nameof(templateId));
        if (grantedBy == null)
            throw new ArgumentException("GrantedBy cannot be null", nameof(grantedBy));

        AddTemplatePermissionToUser(
            user,
            templateId.Value.ToString(),
            new[] { AccessType.Read, AccessType.Write },
            grantedBy,
            grantedOn);
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