using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using MockQueryable;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users;

public class GetApplicationIdsByUserIdQueryObjectTests
{
    [Fact]
    public void Apply_ShouldReturnDistinctApplicationScopedPermissionsOnly()
    {
        var userId = new UserId(Guid.NewGuid());
        var otherUserId = new UserId(Guid.NewGuid());
        var appId1 = new ApplicationId(Guid.NewGuid());
        var appId2 = new ApplicationId(Guid.NewGuid());

        var permissions = new List<Permission>
        {
            new(new PermissionId(Guid.NewGuid()), userId, appId1, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, userId),
            new(new PermissionId(Guid.NewGuid()), userId, appId1, "Application:Write", ResourceType.Application, AccessType.Write, DateTime.UtcNow, userId),
            new(new PermissionId(Guid.NewGuid()), userId, appId2, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, userId),
            new(new PermissionId(Guid.NewGuid()), userId, null, "Template:Read", ResourceType.Template, AccessType.Read, DateTime.UtcNow, userId),
            new(new PermissionId(Guid.NewGuid()), otherUserId, appId1, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, otherUserId),
        }.AsQueryable().BuildMock();

        var result = new GetApplicationIdsByUserIdQueryObject(userId)
            .Apply(permissions)
            .Select(p => p.ApplicationId!)
            .Distinct()
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(appId1, result);
        Assert.Contains(appId2, result);
    }
}
