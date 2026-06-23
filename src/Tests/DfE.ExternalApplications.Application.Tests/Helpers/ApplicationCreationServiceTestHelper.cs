using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Helpers;

internal static class ApplicationCreationServiceTestHelper
{
    public static IApplicationCreationService MockReturning(
        Domain.Entities.Application application,
        ApplicationResponse? response = null)
    {
        var service = Substitute.For<IApplicationCreationService>();
        service.CreateAsync(
                Arg.Any<TemplateVersionId>(),
                Arg.Any<string>(),
                Arg.Any<UserId>(),
                Arg.Any<CancellationToken>())
            .Returns((application, response!));
        return service;
    }
}
