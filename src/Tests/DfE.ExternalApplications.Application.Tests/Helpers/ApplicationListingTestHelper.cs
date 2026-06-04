using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Helpers;

internal static class ApplicationListingTestHelper
{
    internal static ITenantTemplateResolver CreateEmptyTemplateResolver()
    {
        var resolver = Substitute.For<ITenantTemplateResolver>();
        resolver.ResolveListingTemplateFilter(Arg.Any<Guid?>()).Returns(Array.Empty<TemplateId>());
        return resolver;
    }

    internal static ITenantTemplateResolver CreateTemplateResolver(params TemplateId[] allowedTemplateIds)
    {
        var allowed = allowedTemplateIds.ToHashSet();
        var resolver = Substitute.For<ITenantTemplateResolver>();
        resolver.ResolveListingTemplateFilter(Arg.Any<Guid?>())
            .Returns(call =>
            {
                var requested = call.Arg<Guid?>();
                if (requested.HasValue)
                {
                    var templateId = new TemplateId(requested.Value);
                    return allowed.Contains(templateId)
                        ? new[] { templateId }
                        : Array.Empty<TemplateId>();
                }

                return allowed.ToList().AsReadOnly();
            });
        return resolver;
    }

    internal static void AttachTemplateVersion(
        Domain.Entities.Application application,
        TemplateId templateId,
        UserId createdBy)
    {
        var template = new Template(templateId, "Test Template", DateTime.UtcNow, createdBy);
        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            templateId,
            "1.0",
            "{}",
            DateTime.UtcNow,
            createdBy);
        templateVersion.GetType().GetProperty(nameof(TemplateVersion.Template))?.SetValue(templateVersion, template);
        application.GetType().GetProperty(nameof(Domain.Entities.Application.TemplateVersion))
            ?.SetValue(application, templateVersion);
    }
}
