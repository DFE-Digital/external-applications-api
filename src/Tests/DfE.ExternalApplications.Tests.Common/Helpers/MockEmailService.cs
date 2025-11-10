using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock email service that returns successful responses for testing purposes
/// </summary>
public class MockEmailService : IEmailService
{
    public string ProviderName => throw new NotImplementedException();

    public Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(string? templateType = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<EmailResponse>> GetEmailsAsync(string? reference = null, EmailStatus? status = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<EmailTemplate> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<EmailTemplate> GetTemplateAsync(string templateId, int version, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public bool IsValidEmail(string emailAddress)
    {
        throw new NotImplementedException();
    }

    public Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<EmailResponse> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // Return a successful response without actually sending an email
        var response = new EmailResponse
        {
            Id = Guid.NewGuid().ToString(),
            Status = EmailStatus.Sent
        };
        
        return Task.FromResult(response);
    }
}

