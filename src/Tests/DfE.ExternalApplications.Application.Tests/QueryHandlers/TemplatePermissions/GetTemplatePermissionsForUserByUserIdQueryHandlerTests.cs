//using AutoFixture;
//using AutoFixture.Xunit2;
//using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
//using DfE.ExternalApplications.Domain.Entities;
//using DfE.ExternalApplications.Domain.Interfaces.Repositories;
//using DfE.ExternalApplications.Domain.ValueObjects;
//using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
//using MockQueryable;
//using MockQueryable.NSubstitute;
//using NSubstitute;
//using NSubstitute.ExceptionExtensions;

//namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.TemplatePermissions;

//public class GetTemplatePermissionsForUserByUserIdQueryHandlerTests
//{
//    [Theory]
//    [CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
//    public async Task Handle_ShouldReturnTemplatePermissions_WhenUserExists(
//        UserId userId,
//        UserCustomization userCustom,
//        TemplatePermissionCustomization permCustom,
//        [Frozen] IEaRepository<User> userRepo)
//    {
//        // Arrange
//        userCustom.OverrideId = userId;
//        var fixture = new Fixture().Customize(userCustom);
//        var user = fixture.Create<User>();

//        var backingField = typeof(User)
//            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
//        backingField.SetValue(user, new List<TemplatePermission>());

//        var templatePermission = new Fixture().Customize(permCustom).Create<TemplatePermission>();
//        ((List<TemplatePermission>)backingField.GetValue(user)!).Add(templatePermission);

//        var userList = new List<User> { user };
//        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(userRepo);

//        // Act
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.Single(result.Value!);
//        Assert.Equal(templatePermission.Id!.Value, result.Value!.First().TemplatePermissionId);
//    }

//    [Theory]
//    [CustomAutoData(typeof(UserCustomization))]
//    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
//        UserId userId,
//        UserCustomization userCustom,
//        [Frozen] IEaRepository<User> userRepo)
//    {
//        // Arrange
//        userCustom.OverrideId = new UserId(Guid.NewGuid());
//        var user = new Fixture().Customize(userCustom).Create<User>();
//        var userQ = new List<User> { user }.AsQueryable().BuildMock();
//        userRepo.Query().Returns(userQ);

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(userRepo);

//        // Act
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.Empty(result.Value!);
//    }

//    [Theory]
//    [CustomAutoData]
//    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs(
//        UserId userId,
//        [Frozen] IEaRepository<User> userRepo)
//    {
//        // Arrange
//        userRepo.Query().Throws(new Exception("Boom"));

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(userRepo);

//        // Act
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.False(result.IsSuccess);
//        Assert.Contains("Boom", result.Error);
//    }
//} 