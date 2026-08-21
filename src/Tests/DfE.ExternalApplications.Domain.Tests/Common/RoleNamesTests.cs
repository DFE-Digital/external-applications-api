using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Domain.Tests.Common;

public class RoleNamesTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("SuperAdmin")]
    [InlineData("superadmin")]
    public void IsAdminName_ShouldReturnTrue_ForAdminAliases(string roleName)
    {
        Assert.True(RoleNames.IsAdminName(roleName));
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Caseworker")]
    [InlineData(null)]
    [InlineData("")]
    public void IsAdminName_ShouldReturnFalse_ForNonAdminRoles(string? roleName)
    {
        Assert.False(RoleNames.IsAdminName(roleName));
    }

    [Theory]
    [InlineData("SuperAdmin", "Admin")]
    [InlineData("Admin", "Admin")]
    [InlineData("User", "User")]
    [InlineData("Caseworker", "Caseworker")]
    public void ToClaimRole_ShouldNormalizeSuperAdminToAdmin(string input, string expected)
    {
        Assert.Equal(expected, RoleNames.ToClaimRole(input));
    }

    [Fact]
    public void ResolveAssignable_ShouldMapSuperAdminToAdmin()
    {
        Assert.Equal(RoleNames.Admin, RoleNames.ResolveAssignable("SuperAdmin"));
        Assert.True(RoleNames.IsAssignable("SuperAdmin"));
    }

    [Fact]
    public void IsDowngradeToUser_ShouldTreatSuperAdminAsAdmin()
    {
        Assert.True(RoleNames.IsDowngradeToUser("SuperAdmin", RoleNames.User));
        Assert.False(RoleNames.IsDowngradeToUser("SuperAdmin", RoleNames.Caseworker));
    }
}
