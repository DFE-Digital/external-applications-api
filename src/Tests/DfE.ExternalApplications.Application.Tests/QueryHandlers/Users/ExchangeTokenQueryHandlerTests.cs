using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Security.Interfaces;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;
using DfE.CoreLibs.Security.Models;
using DfE.ExternalApplications.Domain.ValueObjects;
using MockQueryable;
using Microsoft.AspNetCore.Authentication;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Users;

public class ExchangeTokenQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ValidToken_ShouldReturnExchangeTokenDto(
        string subjectToken,
        string email,
        UserCustomization userCustom,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        externalValidator.ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var role = new Role(user.RoleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);
        
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        // Set up mock HttpContext with authentication service
        var httpContext = Substitute.For<HttpContext>();
        var authService = Substitute.For<IAuthenticationService>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        var authResult = AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "TestRole") })),
            "AzureEntra"));
        authService.AuthenticateAsync(httpContext, "AzureEntra").Returns(authResult);
        serviceProvider.GetService(typeof(IAuthenticationService)).Returns(authService);
        httpContext.RequestServices.Returns(serviceProvider);
        httpContextAccessor.HttpContext.Returns(httpContext);

        var expectedInternalToken = new Token
        {
            AccessToken = "internal-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };
        tokenService
            .GetUserTokenModelAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult(expectedInternalToken));

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await externalValidator.Received(1).ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>());
        await tokenService.Received(1).GetUserTokenModelAsync(Arg.Is<ClaimsPrincipal>(p => p.HasClaim(ClaimTypes.Role, user.Role.Name)));
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserWithNoRole_ShouldThrowException(
        string subjectToken,
        string email,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        externalValidator.ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "test user", email, DateTime.UtcNow,
            null, null, null);
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);
        
        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
                   () => handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None));
        Assert.Equal($"ExchangeTokenQueryHandler > User {email} has no role assigned", exception.Message);
    }


    [Theory]
    [CustomAutoData]
    public async Task Handle_MissingEmail_ShouldThrowSecurityTokenException(
        string subjectToken,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor)
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()));
        externalValidator.ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None));
        Assert.Equal("ExchangeTokenQueryHandler > Missing email", exception.Message);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_UserNotFound_ShouldThrowSecurityTokenException(
        string subjectToken,
        string email,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        externalValidator.ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var emptyQueryable = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyQueryable);

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None));
        Assert.Equal($"ExchangeTokenQueryHandler > User not found for email {email}", exception.Message);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldThrow_WhenValidationFails(
        string subjectToken,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenSvc,
        [Frozen] IHttpContextAccessor httpCtxAcc)
    {
        // Arrange
        var exception = new SecurityTokenException("Invalid token");
        externalValidator.ValidateIdTokenAsync(subjectToken, Arg.Any<CancellationToken>())
            .Throws(exception);

        var handler = new ExchangeTokenQueryHandler(externalValidator, userRepo, tokenSvc, httpCtxAcc);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SecurityTokenException>(
            () => handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None));
        Assert.Equal("Invalid token", ex.Message);
    }
} 