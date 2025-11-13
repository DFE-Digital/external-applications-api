using DfE.ExternalApplications.Application.Applications.Commands;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.UserFeedback;

public class SubmitUserFeedbackCommandValidatorTests
{
    [Theory]
    [InlineData(UserFeedbackType.BugReport, null, null, null)]
    [InlineData(UserFeedbackType.SupportRequest, "ABC-20001231-001", "another.email@education.gov.uk", null)]
    [InlineData(UserFeedbackType.FeedbackOrSuggestion, null, null, SatisfactionScore.Satisfied)]
    public void Validate_should_fail_when_message_is_empty(UserFeedbackType type, string? referenceNumber,
        string? emailAddress, SatisfactionScore? satisfactionScore)
    {
        var templateId = Guid.NewGuid();
        var request = GetUserFeedbackRequest(type, "", templateId, referenceNumber, emailAddress, satisfactionScore);
        var command = new SubmitUserFeedbackCommand(request);
        var validator = new SubmitUserFeedbackCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Message is required", result.Errors[0].ErrorMessage);
        Assert.Equal("Request.Message", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData(null, "some.email@education.gov.uk")]
    [InlineData("", "another.email@education.gov.uk")]
    [InlineData("   ", "yet.another.email.address@education.gov.uk")]
    public void Validate_should_fail_when_reference_number_is_null_or_empty(string? referenceNumber,
        string emailAddress)
    {
        var templateId = Guid.NewGuid();
        var request = new SupportRequest("message", referenceNumber!, emailAddress, templateId);
        var command = new SubmitUserFeedbackCommand(request);
        var validator = new SubmitUserFeedbackCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Reference number is required", result.Errors[0].ErrorMessage);
        Assert.Equal("Request.ReferenceNumber", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData(UserFeedbackType.BugReport, null, "")]
    [InlineData(UserFeedbackType.SupportRequest, "ABC-20001231-001", "not an email")]
    public void Validate_should_fail_when_email_is_invalid(UserFeedbackType type, string? referenceNumber,
        string emailAddress)
    {
        var templateId = Guid.NewGuid();
        var request = GetUserFeedbackRequest(type, "message", templateId, referenceNumber, emailAddress);
        var command = new SubmitUserFeedbackCommand(request);
        var validator = new SubmitUserFeedbackCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Email address must be a valid email address", result.Errors[0].ErrorMessage);
        Assert.Equal("Request.EmailAddress", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void Validate_should_fail_when_satisfaction_score_is_invalid(int satisfactionScore)
    {
        var templateId = Guid.NewGuid();
        var request = new FeedbackOrSuggestion("message", null, (SatisfactionScore)satisfactionScore, templateId);
        var command = new SubmitUserFeedbackCommand(request);
        var validator = new SubmitUserFeedbackCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Satisfaction score must be a valid value", result.Errors[0].ErrorMessage);
        Assert.Equal("Request.SatisfactionScore", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData(UserFeedbackType.BugReport, null, null, null)]
    [InlineData(UserFeedbackType.SupportRequest, "ABC-20001231-001", "another.email@education.gov.uk", null)]
    [InlineData(UserFeedbackType.FeedbackOrSuggestion, null, null, SatisfactionScore.Satisfied)]
    public void Validate_should_succeed_when_request_is_valid(UserFeedbackType type, string? referenceNumber,
        string? emailAddress, SatisfactionScore? satisfactionScore)
    {
        var templateId = Guid.NewGuid();
        var request =
            GetUserFeedbackRequest(type, "message", templateId, referenceNumber, emailAddress, satisfactionScore);
        var command = new SubmitUserFeedbackCommand(request);
        var validator = new SubmitUserFeedbackCommandValidator();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    private static UserFeedbackRequest GetUserFeedbackRequest(UserFeedbackType type, string message, Guid templateId,
        string? referenceNumber = null, string? emailAddress = null, SatisfactionScore? satisfactionScore = null)
    {
        switch (type)
        {
            case UserFeedbackType.BugReport:
                return new BugReport(message, referenceNumber, emailAddress, templateId);

            case UserFeedbackType.SupportRequest:
                ArgumentNullException.ThrowIfNull(referenceNumber);
                ArgumentNullException.ThrowIfNull(emailAddress);
                return new SupportRequest(message, referenceNumber, emailAddress, templateId);

            case UserFeedbackType.FeedbackOrSuggestion:
                ArgumentNullException.ThrowIfNull(satisfactionScore);
                return new FeedbackOrSuggestion(message, referenceNumber, (SatisfactionScore)satisfactionScore,
                    templateId);

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}