using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class TenantTemplateResolverTests
{
    private static readonly TemplateId TransferTemplateId = new(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
    private static readonly TemplateId LsrpTemplateId = new(Guid.Parse("B2F8E7D4-2C46-4A91-8E73-9D5A1F4B6C89"));

    [Fact]
    public void ResolveListingTemplateFilter_ShouldReturnRequestedTemplate_WhenItBelongsToTenant()
    {
        var resolver = CreateResolver(CreateTransferTenantSettings());

        var result = resolver.ResolveListingTemplateFilter(TransferTemplateId.Value);

        Assert.Single(result);
        Assert.Equal(TransferTemplateId, result[0]);
    }

    [Fact]
    public void ResolveListingTemplateFilter_ShouldReturnEmpty_WhenRequestedTemplateIsOutsideTenant()
    {
        var resolver = CreateResolver(CreateTransferTenantSettings());

        var result = resolver.ResolveListingTemplateFilter(LsrpTemplateId.Value);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTemplateIdsForCurrentTenant_ShouldReturnOnlyConfiguredTemplates()
    {
        var resolver = CreateResolver(CreateTransferTenantSettings());

        var templateIds = resolver.GetTemplateIdsForCurrentTenant();

        Assert.Single(templateIds);
        Assert.Equal(TransferTemplateId, templateIds[0]);
    }

    private static TenantTemplateResolver CreateResolver(IConfigurationRoot settings)
    {
        var tenant = new TenantConfiguration(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "Transfers",
            settings,
            Array.Empty<string>());

        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.CurrentTenant.Returns(tenant);

        return new TenantTemplateResolver(accessor, NullLogger<TenantTemplateResolver>.Instance);
    }

    private static IConfigurationRoot CreateTransferTenantSettings() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationTemplates:HostMappings:transfers"] = TransferTemplateId.Value.ToString()
            })
            .Build();
}
