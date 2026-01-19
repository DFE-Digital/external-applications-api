using System.Linq.Expressions;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.UserFeedback;

public class SubmitUserFeedbackHandlerTests
{
    private readonly IFixture _fixture;

    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IEmailTemplateResolver _emailTemplateResolver;
    private readonly IEmailService _emailService;

    private readonly SubmitUserFeedbackCommandHandler _handler;

    public SubmitUserFeedbackHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });

        var logger = _fixture.Create<ILogger<SubmitUserFeedbackCommandHandler>>();

        _tenantContextAccessor = _fixture.Create<ITenantContextAccessor>();
        _fixture.Inject(_tenantContextAccessor);

        _emailTemplateResolver = _fixture.Create<IEmailTemplateResolver>();
        _fixture.Inject(_emailTemplateResolver);

        _emailService = _fixture.Create<IEmailService>();
        _fixture.Inject(_emailService);

        _fixture.Register(() => _fixture.Build<EmailResponse>().Without(r => r.RecipientResponses).Create());

        _handler = new SubmitUserFeedbackCommandHandler(logger, _tenantContextAccessor, _emailTemplateResolver,
            _emailService);
    }

    [Theory]
    [InlineData(typeof(BugReport), $"{nameof(BugReport)}Internal")]
    [InlineData(typeof(SupportRequest), $"{nameof(SupportRequest)}Internal")]
    [InlineData(typeof(FeedbackOrSuggestion), $"{nameof(FeedbackOrSuggestion)}Internal")]
    public async Task Handle_sends_message_to_configured_service_support_email_address(Type userFeedbackType,
        string emailType)
    {
        var supportEmailAddress = $"{_fixture.Create<string>()}@education.gov.uk";
        SetupTenantContextWithSupportEmail(supportEmailAddress);

        var eaTemplateId = _fixture.Create<TemplateId>();
        var emailTemplateId = _fixture.Create<string>();
        _emailTemplateResolver.ResolveEmailTemplateAsync(eaTemplateId, emailType).Returns(emailTemplateId);

        RegisterUserFeedbackRequests(eaTemplateId);
        var request = (UserFeedbackRequest)_fixture.Create(userFeedbackType, new SpecimenContext(_fixture));
        var cancellationToken = _fixture.Create<CancellationToken>();

        var result = await _handler.Handle(new SubmitUserFeedbackCommand(request), cancellationToken);

        Assert.True(result.IsSuccess);

        await _emailTemplateResolver.Received().ResolveEmailTemplateAsync(eaTemplateId, emailType);
        var expectedEmail = GetExpectedEmail(request, supportEmailAddress, emailTemplateId);
        await _emailService.Received().SendEmailAsync(Arg.Is(MatchFor(expectedEmail)), cancellationToken);
    }

    [Theory]
    [InlineData(typeof(BugReport), $"{nameof(BugReport)}Internal")]
    [InlineData(typeof(SupportRequest), $"{nameof(SupportRequest)}Internal")]
    [InlineData(typeof(FeedbackOrSuggestion), $"{nameof(FeedbackOrSuggestion)}Internal")]
    public async Task Handle_does_not_send_message_to_service_support_email_address_if_not_configured(
        Type userFeedbackType, string emailType)
    {
        var supportEmailAddress = $"{_fixture.Create<string>()}@education.gov.uk";
        SetupTenantContextWithSupportEmail(null);

        var eaTemplateId = _fixture.Create<TemplateId>();
        var emailTemplateId = _fixture.Create<string>();
        _emailTemplateResolver.ResolveEmailTemplateAsync(eaTemplateId, emailType).Returns(emailTemplateId);

        RegisterUserFeedbackRequests(eaTemplateId);
        var request = (UserFeedbackRequest)_fixture.Create(userFeedbackType, new SpecimenContext(_fixture));
        var cancellationToken = _fixture.Create<CancellationToken>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SubmitUserFeedbackCommand(request), cancellationToken));

        Assert.Contains("Service support email address is not configured", ex.Message);

        await _emailService.DidNotReceive().SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == supportEmailAddress),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(typeof(BugReport), $"{nameof(BugReport)}User")]
    [InlineData(typeof(SupportRequest), $"{nameof(SupportRequest)}User")]
    public async Task Handle_sends_message_to_user_provided_email_address(Type userFeedbackType, string emailType)
    {
        var userEmailAddress = $"{_fixture.Create<string>()}@{_fixture.Create<string>()}.co.uk";

        var eaTemplateId = _fixture.Create<TemplateId>();
        var emailTemplateId = _fixture.Create<string>();
        _emailTemplateResolver.ResolveEmailTemplateAsync(eaTemplateId, emailType).Returns(emailTemplateId);

        RegisterUserFeedbackRequests(eaTemplateId, userEmailAddress);
        var request = (UserFeedbackRequest)_fixture.Create(userFeedbackType, new SpecimenContext(_fixture));
        var cancellationToken = _fixture.Create<CancellationToken>();

        var result = await _handler.Handle(new SubmitUserFeedbackCommand(request), cancellationToken);

        Assert.True(result.IsSuccess);

        await _emailTemplateResolver.Received().ResolveEmailTemplateAsync(eaTemplateId, emailType);
        var expectedEmail = GetExpectedEmail(request, userEmailAddress, emailTemplateId);
        await _emailService.Received().SendEmailAsync(Arg.Is(MatchFor(expectedEmail)), cancellationToken);
    }

    [Theory]
    [InlineData(typeof(BugReport), $"{nameof(BugReport)}User")]
    [InlineData(typeof(FeedbackOrSuggestion), $"{nameof(FeedbackOrSuggestion)}User")]
    public async Task Handle_does_not_send_user_message_if_email_address_is_not_provided(Type userFeedbackType,
        string emailType)
    {
        var supportEmailAddress = $"{_fixture.Create<string>()}@education.gov.uk";
        SetupTenantContextWithSupportEmail(supportEmailAddress);

        var eaTemplateId = _fixture.Create<TemplateId>();
        var emailTemplateId = _fixture.Create<string>();
        _emailTemplateResolver.ResolveEmailTemplateAsync(eaTemplateId, emailType).Returns(emailTemplateId);

        RegisterUserFeedbackRequests(eaTemplateId, null);
        var request = (UserFeedbackRequest)_fixture.Create(userFeedbackType, new SpecimenContext(_fixture));

        var cancellationToken = _fixture.Create<CancellationToken>();

        var result = await _handler.Handle(new SubmitUserFeedbackCommand(request), cancellationToken);

        Assert.True(result.IsSuccess);

        await _emailService.DidNotReceive().SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail != supportEmailAddress),
            Arg.Any<CancellationToken>());
    }

    private static Expression<Predicate<EmailMessage>> MatchFor(EmailMessage expected) => actual =>
        expected.TemplateId == actual.TemplateId &&
        expected.ToEmail == actual.ToEmail &&
        PersonalizationMatches(expected.Personalization, actual.Personalization);

    private static bool PersonalizationMatches(Dictionary<string, object>? expected, Dictionary<string, object>? actual)
    {
        if (expected is null) return actual is null;

        return actual is not null && expected.All(kv => actual.ContainsKey(kv.Key) && kv.Value.Equals(actual[kv.Key]));
    }

    private void RegisterUserFeedbackRequests(TemplateId templateId) =>
        RegisterUserFeedbackRequests(templateId, _fixture.Create<string>());

    private void RegisterUserFeedbackRequests(TemplateId templateId, string? emailAddress)
    {
        _fixture.Register(() =>
            _fixture.Build<BugReport>()
                .With(br => br.TemplateId, templateId.Value)
                .With(br => br.EmailAddress, emailAddress)
                .Create());

        _fixture.Register(() =>
            _fixture.Build<SupportRequest>()
                .With(sr => sr.TemplateId, templateId.Value)
                .With(sr => sr.EmailAddress, emailAddress)
                .Create());

        _fixture.Register(() =>
            _fixture.Build<FeedbackOrSuggestion>()
                .With(f => f.TemplateId, templateId.Value)
                .Create());
    }

    private static EmailMessage GetExpectedEmail(UserFeedbackRequest request, string emailAddress,
        string emailTemplateId)
    {
        var expectedPersonalization = new Dictionary<string, object>
        {
            ["message"] = request.Message,
            ["reference_number"] = request.ReferenceNumber ?? "Not provided",
        };
        if (request is FeedbackOrSuggestion feedback)
        {
            expectedPersonalization.Add("satisfaction_score", ExpectedSatisfactionScore(feedback.SatisfactionScore));
        }

        var expectedEmail = new EmailMessage()
        {
            ToEmail = emailAddress,
            TemplateId = emailTemplateId,
            Personalization = expectedPersonalization
        };
        return expectedEmail;
    }

    private static string ExpectedSatisfactionScore(SatisfactionScore score) => score switch
    {
        SatisfactionScore.VerySatisfied => "Very satisfied",
        SatisfactionScore.Satisfied => "Satisfied",
        SatisfactionScore.NeitherSatisfiedOrDissatisfied => "Neither satisfied or dissatisfied",
        SatisfactionScore.Dissatisfied => "Dissatisfied",
        SatisfactionScore.VeryDissatisfied => "Very dissatisfied",
        _ => throw new ArgumentOutOfRangeException(nameof(score), score, null)
    };

    private void SetupTenantContextWithSupportEmail(string? supportEmailAddress)
    {
        var mockConfiguration = Substitute.For<IConfiguration>();
        mockConfiguration.GetValue<string?>("Email:ServiceSupportEmailAddress", Arg.Any<string?>()).Returns(supportEmailAddress);
        
        var mockTenantConfiguration = Substitute.For<TenantConfiguration>(
            Guid.NewGuid(),
            "TestTenant",
            mockConfiguration);
        mockTenantConfiguration.Settings.Returns(mockConfiguration);
        
        _tenantContextAccessor.CurrentTenant.Returns(mockTenantConfiguration);
    }
}