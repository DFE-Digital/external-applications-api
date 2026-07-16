using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.Templates.Commands;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Templates;

public class CreateTemplateCommandHandlerTests
{
    private readonly IEaRepository<Template> _templateRepo = Substitute.For<IEaRepository<Template>>();
    private readonly IEaRepository<User> _userRepo = Substitute.For<IEaRepository<User>>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IPermissionCheckerService _permissionChecker = Substitute.For<IPermissionCheckerService>();
    private readonly ITemplateFactory _templateFactory = Substitute.For<ITemplateFactory>();
    private readonly IUserFactory _userFactory = Substitute.For<IUserFactory>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateTemplateCommandHandler _handler;

    public CreateTemplateCommandHandlerTests()
    {
        _handler = new CreateTemplateCommandHandler(
            _templateRepo,
            _userRepo,
            _httpContextAccessor,
            _permissionChecker,
            _templateFactory,
            _userFactory,
            _cacheInvalidator,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldForbid_WhenCallerIsNotAdmin()
    {
        _permissionChecker.IsAdmin().Returns(false);

        var result = await _handler.Handle(new CreateTemplateCommand("Transfers"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Admin", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldCreateTemplate_WhenAdmin()
    {
        _permissionChecker.IsAdmin().Returns(true);

        var userId = new UserId(Guid.NewGuid());
        var email = "admin@education.gov.uk";
        var user = new User(userId, new RoleId(Guid.NewGuid()), "Admin", email, DateTime.UtcNow, null, null, null);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "Bearer")));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var users = new List<User> { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        var templateId = new TemplateId(Guid.NewGuid());
        var template = new Template(templateId, "New Template", DateTime.UtcNow, userId);
        _templateFactory.CreateTemplate("New Template", userId, Arg.Any<DateTime?>()).Returns(template);

        var result = await _handler.Handle(new CreateTemplateCommand("New Template"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(templateId.Value, result.Value!.TemplateId);
        Assert.Equal("New Template", result.Value.Name);
        await _templateRepo.Received(1).AddAsync(template, Arg.Any<CancellationToken>());
        _userFactory.Received(1).EnsureUserHasTemplatePermission(user, templateId, userId, Arg.Any<DateTime?>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _cacheInvalidator.Received(1).InvalidateForUserAsync(
            email,
            Arg.Any<string?>(),
            userId,
            Arg.Any<CancellationToken>());
    }
}
