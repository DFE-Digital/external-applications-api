using AutoFixture;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Api;
using DfE.ExternalApplications.Tests.Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

namespace DfE.ExternalApplications.Tests.Common.Customizations;

public class NotificationTestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // First apply the base customization
        var baseCustomization = new CustomWebApplicationDbContextFactoryCustomization();
        baseCustomization.Customize(fixture);

        // Then modify the factory to use our mock notification service
        fixture.Customize<CustomWebApplicationDbContextFactory<Program>>(composer =>
            composer.Do(factory =>
            {
                // Extend the existing ExternalServicesConfiguration
                var originalExternalServices = factory.ExternalServicesConfiguration;
                factory.ExternalServicesConfiguration = services =>
                {
                    // Apply the original configuration first
                    originalExternalServices?.Invoke(services);

                    // Replace the notification service with our mock
                    services.RemoveAll<DfE.CoreLibs.Notifications.Interfaces.INotificationService>();
                    services.AddSingleton<DfE.CoreLibs.Notifications.Interfaces.INotificationService, MockNotificationService>();
                };
            }));
    }
}
