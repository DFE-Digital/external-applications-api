using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public interface IUserFactory
{
    User CreateContributor(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        UserId createdBy,
        ApplicationId applicationId,
        DateTime? createdOn = null);
} 