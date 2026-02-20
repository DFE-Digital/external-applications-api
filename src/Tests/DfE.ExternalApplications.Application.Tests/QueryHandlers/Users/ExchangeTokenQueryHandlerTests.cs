using AutoFixture;
using AutoFixture.Xunit2;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Users;

public class ExchangeTokenQueryHandlerTests
{
    private static (TenantConfiguration Tenant, Guid TemplateId) CreateTenantWithSingleTemplate()
    {
        var templateId = Guid.NewGuid();
        var configData = new Dictionary<string, string?>
        {
            ["ApplicationTemplates:HostMappings:transfer"] = templateId.ToString()
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var tenant = new TenantConfiguration(
            Guid.NewGuid(),
            "TestTenant",
            configuration,
            Array.Empty<string>());
        return (tenant, templateId);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ValidToken_ShouldReturnExchangeTokenDto(
        string subjectToken,
        string email,
        UserCustomization userCustom,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange
        var (tenant, templateId) = CreateTenantWithSingleTemplate();
        tenantContextAccessor.CurrentTenant.Returns(tenant);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        userCustom.OverrideEmail = email;
        userCustom.OverrideTemplatePermissions = new[]
        {
            new TemplatePermission(
                new TemplatePermissionId(Guid.NewGuid()),
                new UserId(Guid.NewGuid()),
                new TemplateId(templateId),
                AccessType.Read,
                DateTime.UtcNow,
                new UserId(Guid.NewGuid()))
        };
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
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await externalValidator.Received(1).ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>());
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
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange
        var (tenant, _) = CreateTenantWithSingleTemplate();
        tenantContextAccessor.CurrentTenant.Returns(tenant);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "test user", email, DateTime.UtcNow,
            null, null, null);
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);
        
        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"User {email} has no role assigned", result.Error);
    }


    [Theory]
    [CustomAutoData]
    public async Task Handle_MissingEmail_ShouldReturnFailure(
        string subjectToken,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()));
        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Missing email", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_UserNotFound_ShouldThrowSecurityTokenException(
        string subjectToken,
        string email,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange
        var (tenant, _) = CreateTenantWithSingleTemplate();
        tenantContextAccessor.CurrentTenant.Returns(tenant);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Handler calls 5-arg overload (validInternalAuthReq, tenantInternalAuthOptions)
        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(claimsPrincipal);

        var emptyQueryable = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyQueryable);

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"User not found for email {email}", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldThrow_WhenValidationFails(
        string subjectToken,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenSvc,
        [Frozen] IHttpContextAccessor httpCtxAcc,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange - when validation throws, handler should let SecurityTokenException propagate
        var exception = new SecurityTokenException("Invalid token");
        var faultedTask = Task.FromException<ClaimsPrincipal>(exception);
        // Handler calls 5-arg overload; set up both overloads so the mock is used regardless of which is bound
        externalValidator.ValidateIdTokenAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(faultedTask);
        externalValidator.ValidateIdTokenAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<InternalServiceAuthOptions?>(), Arg.Any<CancellationToken>())
            .Returns(faultedTask);

        var handler = new ExchangeTokenQueryHandler(externalValidator, userRepo, tokenSvc, httpCtxAcc, tenantContextAccessor, internalRequestChecker, logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SecurityTokenException>(
            () => handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None));
        Assert.Equal("Invalid token", ex.Message);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserWithoutTemplateAccess_ReturnsNotFound(
        string subjectToken,
        string email,
        UserCustomization userCustom,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange: tenant has template A; user exists but has permission only for a different template B
        var (tenant, requestTemplateId) = CreateTenantWithSingleTemplate();
        tenantContextAccessor.CurrentTenant.Returns(tenant);

        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(claims)));

        userCustom.OverrideEmail = email;
        var otherTemplateId = Guid.NewGuid(); // different from requestTemplateId
        userCustom.OverrideTemplatePermissions = new[]
        {
            new TemplatePermission(
                new TemplatePermissionId(Guid.NewGuid()),
                new UserId(Guid.NewGuid()),
                new TemplateId(otherTemplateId),
                AccessType.Read,
                DateTime.UtcNow,
                new UserId(Guid.NewGuid()))
        };
        var user = new Fixture().Customize(userCustom).Create<User>();
        var role = new Role(user.RoleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);

        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert: treated as "user not found" so client triggers auto-registration
        Assert.False(result.IsSuccess);
        Assert.Equal($"User not found for email {email}", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_WhenTenantHasNoTemplateConfig_ReturnsFailure(
        string subjectToken,
        string email,
        [Frozen] IExternalIdentityValidator externalValidator,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IUserTokenService tokenService,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ITenantContextAccessor tenantContextAccessor,
        [Frozen][FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        [Frozen] ILogger<ExchangeTokenQueryHandler> logger)
    {
        // Arrange: tenant has no ApplicationTemplates:HostMappings
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var tenant = new TenantConfiguration(Guid.NewGuid(), "TestTenant", configuration, Array.Empty<string>());
        tenantContextAccessor.CurrentTenant.Returns(tenant);

        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        externalValidator.ValidateIdTokenAsync(subjectToken, false, false, null, Arg.Any<CancellationToken>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(claims)));

        var handler = new ExchangeTokenQueryHandler(
            externalValidator,
            userRepo,
            tokenService,
            httpContextAccessor,
            tenantContextAccessor,
            internalRequestChecker,
            logger);

        // Act
        var result = await handler.Handle(new ExchangeTokenQuery(subjectToken), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Template could not be resolved", result.Error);
    }
} 