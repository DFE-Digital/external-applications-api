using DfE.ExternalApplications.Application.Users.Commands;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Users;

public class AssignUserRoleCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldForbid_WhenCallerIsNotAdmin(
        string email,
        string name,
        IEaRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IPermissionCheckerService permissionCheckerService,
        IUserRoleProvisionerRegistry roleProvisionerRegistry,
        IHttpContextAccessor httpContextAccessor)
    {
        permissionCheckerService.IsAdmin().Returns(false);

        var handler = new AssignUserRoleCommandHandler(
            userRepo,
            unitOfWork,
            permissionCheckerService,
            roleProvisionerRegistry,
            httpContextAccessor);

        var result = await handler.Handle(
            new AssignUserRoleCommand(email, name, RoleNames.Caseworker, [Guid.NewGuid()]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.Forbidden, result.ErrorCode);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldFail_WhenRoleIsNotAssignable(
        string email,
        string name,
        IEaRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IPermissionCheckerService permissionCheckerService,
        IUserRoleProvisionerRegistry roleProvisionerRegistry,
        IHttpContextAccessor httpContextAccessor)
    {
        permissionCheckerService.IsAdmin().Returns(true);

        var handler = new AssignUserRoleCommandHandler(
            userRepo,
            unitOfWork,
            permissionCheckerService,
            roleProvisionerRegistry,
            httpContextAccessor);

        var result = await handler.Handle(
            new AssignUserRoleCommand(email, name, "SuperUser", [Guid.NewGuid()]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not assignable", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldCreateUser_WhenUserDoesNotExist(
        string adminEmail,
        string email,
        string name,
        UserId adminUserId,
        IEaRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IPermissionCheckerService permissionCheckerService,
        IUserRoleProvisioner provisioner,
        IUserRoleProvisionerRegistry roleProvisionerRegistry,
        IHttpContextAccessor httpContextAccessor)
    {
        permissionCheckerService.IsAdmin().Returns(true);

        var templateId = Guid.NewGuid();
        var grantedOn = DateTime.UtcNow;

        var adminUser = new User(
            adminUserId,
            new RoleId(RoleConstants.AdminRoleId),
            "Admin",
            adminEmail,
            grantedOn,
            null,
            null,
            null);

        var createdUser = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.CaseworkerRoleId),
            name,
            email,
            grantedOn,
            adminUserId,
            null,
            null);

        var users = new List<User> { adminUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        SetupHttpContext(httpContextAccessor, adminEmail);

        provisioner.RoleName.Returns(RoleNames.Caseworker);
        provisioner.RequiresTemplateIds.Returns(true);
        provisioner.CreateUser(Arg.Any<RoleAssignmentRequest>()).Returns(createdUser);

        roleProvisionerRegistry.GetProvisioner(RoleNames.Caseworker).Returns(provisioner);

        var handler = new AssignUserRoleCommandHandler(
            userRepo,
            unitOfWork,
            permissionCheckerService,
            roleProvisionerRegistry,
            httpContextAccessor);

        var result = await handler.Handle(
            new AssignUserRoleCommand(email, name, RoleNames.Caseworker, [templateId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(createdUser.Id!.Value, result.Value!.UserId);
        Assert.Contains(RoleNames.Caseworker, result.Value.Authorization!.Roles!);

        await userRepo.Received(1).AddAsync(createdUser, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        provisioner.Received(1).CreateUser(Arg.Any<RoleAssignmentRequest>());
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldAssignRoleToExistingUser(
        string adminEmail,
        string email,
        string name,
        UserId adminUserId,
        UserId existingUserId,
        IEaRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IPermissionCheckerService permissionCheckerService,
        IUserRoleProvisioner provisioner,
        IUserRoleProvisionerRegistry roleProvisionerRegistry,
        IHttpContextAccessor httpContextAccessor)
    {
        permissionCheckerService.IsAdmin().Returns(true);

        var templateId = Guid.NewGuid();
        var grantedOn = DateTime.UtcNow;

        var adminUser = new User(
            adminUserId,
            new RoleId(RoleConstants.AdminRoleId),
            "Admin",
            adminEmail,
            grantedOn,
            null,
            null,
            null);

        var existingUser = new User(
            existingUserId,
            new RoleId(RoleConstants.UserRoleId),
            name,
            email,
            grantedOn,
            null,
            null,
            null);

        var users = new List<User> { adminUser, existingUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        SetupHttpContext(httpContextAccessor, adminEmail);

        provisioner.RoleName.Returns(RoleNames.Caseworker);
        provisioner.RequiresTemplateIds.Returns(true);

        roleProvisionerRegistry.GetProvisioner(RoleNames.Caseworker).Returns(provisioner);

        var handler = new AssignUserRoleCommandHandler(
            userRepo,
            unitOfWork,
            permissionCheckerService,
            roleProvisionerRegistry,
            httpContextAccessor);

        var result = await handler.Handle(
            new AssignUserRoleCommand(email, name, RoleNames.Caseworker, [templateId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingUserId.Value, result.Value!.UserId);

        provisioner.Received(1).AssignToExistingUser(existingUser, Arg.Any<RoleAssignmentRequest>());
        await userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldFail_WhenTemplateIdsRequiredButMissing(
        string email,
        string name,
        IEaRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IPermissionCheckerService permissionCheckerService,
        IUserRoleProvisioner provisioner,
        IUserRoleProvisionerRegistry roleProvisionerRegistry,
        IHttpContextAccessor httpContextAccessor)
    {
        permissionCheckerService.IsAdmin().Returns(true);

        provisioner.RequiresTemplateIds.Returns(true);
        roleProvisionerRegistry.GetProvisioner(RoleNames.Caseworker).Returns(provisioner);

        var handler = new AssignUserRoleCommandHandler(
            userRepo,
            unitOfWork,
            permissionCheckerService,
            roleProvisionerRegistry,
            httpContextAccessor);

        var result = await handler.Handle(
            new AssignUserRoleCommand(email, name, RoleNames.Caseworker, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("template ID", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetupHttpContext(IHttpContextAccessor httpContextAccessor, string adminEmail)
    {
        var claims = new List<Claim> { new(ClaimTypes.Email, adminEmail) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new DefaultHttpContext { User = principal };
        httpContextAccessor.HttpContext.Returns(context);
    }
}
