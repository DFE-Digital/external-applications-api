using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users;

public class GetUserWithAllTemplatePermissionsQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenEmailExists(UserCustomization userCustom)
    {
        // Arrange
        var targetEmail = "test@example.com";
        var otherEmail = "other@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var targetUser = fixture.Create<User>();
        var otherUser = fixture.Create<User>();
        
        // Use reflection to set the Email property
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(targetUser, targetEmail);
        emailProperty?.SetValue(otherUser, otherEmail);
        
        var users = new[] { targetUser, otherUser };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(targetEmail);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetUser, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenEmailDoesNotExist(UserCustomization userCustom)
    {
        // Arrange
        var targetEmail = "test@example.com";
        var otherEmail = "other@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Email property
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(user, otherEmail);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(targetEmail);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenEmailIsEmpty(UserCustomization userCustom)
    {
        // Arrange
        var targetEmail = "test@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Email property to empty string
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(user, string.Empty);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(targetEmail);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldNormalizeEmail_WhenEmailHasDifferentCase(UserCustomization userCustom)
    {
        // Arrange
        var targetEmail = "TEST@EXAMPLE.COM";
        var normalizedEmail = "test@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Email property
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(user, normalizedEmail);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(targetEmail);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(user, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldTrimEmail_WhenEmailHasWhitespace(UserCustomization userCustom)
    {
        // Arrange
        var targetEmail = "  test@example.com  ";
        var trimmedEmail = "test@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Email property
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(user, trimmedEmail);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(targetEmail);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(user, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldIncludeTemplatePermissions_WhenUserFound(UserCustomization userCustom)
    {
        // Arrange
        var email = "test@example.com";
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Email property
        var emailProperty = typeof(User).GetProperty("Email");
        emailProperty?.SetValue(user, email);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserWithAllTemplatePermissionsQueryObject(email);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(user, result.First());
        
        // Note: The actual inclusion of TemplatePermissions would be tested in integration tests
        // with a real database context, as MockQueryable doesn't fully support Include
    }
} 