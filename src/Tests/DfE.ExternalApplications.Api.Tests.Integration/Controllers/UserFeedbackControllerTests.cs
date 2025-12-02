using System.Security.Claims;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Http.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.Mocks.WebApplicationFactory;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class UserFeedbackControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task PostAsync_should_return_202_Accepted_for_valid_request(
        CustomWebApplicationDbContextFactory<Program> factory,
        IUserFeedbackClient userFeedbackClient)
    {
        factory.TestClaims =
        [
            new Claim(ClaimTypes.Email, EaContextSeeder.BobEmail)
        ];

        var request = new BugReport("Some message", "ABC-20001231-001", "some.email@education.gov.uk", Guid.NewGuid());

        await userFeedbackClient.PostAsync(request);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task PostAsync_should_return_400_Bad_Request_for_invalid_data(
        CustomWebApplicationDbContextFactory<Program> factory,
        IUserFeedbackClient userFeedbackClient)
    {
        factory.TestClaims =
        [
            new Claim(ClaimTypes.Email, EaContextSeeder.BobEmail)
        ];

        var request = new SupportRequest("", "ABC-20001231-001", "not-an-email-address", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(() =>
            userFeedbackClient.PostAsync(request));

        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryWithRateLimitingCustomization))]
    public async Task PostAsync_should_return_429_Too_Many_Requests_when_rate_limit_exceeded(
        CustomWebApplicationDbContextFactory<Program> factory,
        IUserFeedbackClient userFeedbackClient)
    {
        factory.TestClaims =
        [
            new Claim(ClaimTypes.Email, EaContextSeeder.BobEmail)
        ];

        var request1 = new BugReport("Some message 1", "ABC-20001231-001", "some.email@education.gov.uk",
            Guid.NewGuid());
        var request2 = new SupportRequest("Some message 2", "ABC-20001231-001", "another.email@education.gov.uk",
            Guid.NewGuid());

        await userFeedbackClient.PostAsync(request1);

        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(() =>
            userFeedbackClient.PostAsync(request2));

        Assert.Equal(429, ex.StatusCode);
        Assert.Contains("Too many requests", ex.Result?.Message ?? "");
    }
}