using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

public class GetLatestTemplateSchemaQueryHandlerTests
{
    [Theory, CustomAutoData(typeof(TemplatePermissionCustomization), typeof(TemplateVersionCustomization))]
    public async Task Handle_ShouldReturnLatestSchema_WhenUserHasAccess(
        TemplatePermissionCustomization utaCustom,
        TemplateVersionCustomization tvCustom,
        [Frozen] IEaRepository<TemplatePermission> accessRepo,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization()).Create<User>();

        utaCustom.OverrideTemplateId = template.Id;
        var fixture = new Fixture().Customize(utaCustom);
        var access = fixture.Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(access, template);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(access, user);

        var userId = access.UserId;
        var userEmail = access.User?.Email;
        var templateId = template.Id.Value;

        var accessQ = new List<TemplatePermission> { access }.AsQueryable().BuildMock();
        accessRepo.Query().Returns(accessQ);

        tvCustom.OverrideTemplateId = template.Id;
        tvCustom.OverrideCreatedOn = DateTime.UtcNow.AddDays(-1);
        var older = new Fixture().Customize(tvCustom).Create<TemplateVersion>();
        var newerCustomization = new TemplateVersionCustomization
        {
            OverrideTemplateId = template.Id,
            OverrideCreatedOn = DateTime.UtcNow
        };
        var newer = new Fixture().Customize(newerCustomization).Create<TemplateVersion>();

        var tvList = new List<TemplateVersion> { older, newer };
        var tvQ = tvList.AsQueryable().BuildMock();
        versionRepo.Query().Returns(tvQ);

        var cacheKey = $"TemplateSchema_{DfE.CoreLibs.Caching.Helpers.CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{userEmail}";
        cacheService
            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), nameof(GetLatestTemplateSchemaQueryHandler))
            .Returns(call =>
            {
                var factory = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return factory();
            });

        var handler = new GetLatestTemplateSchemaQueryHandler(accessRepo, versionRepo, cacheService);
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId, userEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newer.JsonSchema, result.Value!.JsonSchema);
        Assert.Equal(newer.VersionNumber, result.Value!.VersionNumber);
    }
}
