using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Events;
using Microsoft.Extensions.Logging;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class ApplicationSubmittedEventHandler(
    ILogger<ApplicationSubmittedEventHandler> logger,
    IEmailService emailService,
    IEmailTemplateResolver emailTemplateResolver) : BaseEventHandler<ApplicationSubmittedEvent>(logger)
{
    private readonly ILogger<ApplicationSubmittedEventHandler> _logger = logger;

    protected override async Task HandleEvent(ApplicationSubmittedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the email template ID based on the application template and email type
            var emailTemplateId = await emailTemplateResolver.ResolveEmailTemplateAsync(
                notification.TemplateId,
                "ApplicationSubmitted");

            if (string.IsNullOrEmpty(emailTemplateId))
            {
                _logger.LogError("Could not resolve email template for application {ApplicationId} with template {TemplateId}", 
                    notification.ApplicationId.Value, notification.TemplateId.Value);
                return;
            }

            var email = new EmailMessage()
            {
                ToEmail = notification.UserEmail,
                TemplateId = emailTemplateId,
                Personalization = new Dictionary<string, object>
                {
                    ["user_full_name"] = notification.UserFullName,
                    ["application_reference"] = notification.ApplicationReference,
                    ["submitted_date"] = notification.SubmittedOn.ToString("dd/MM/yyyy"),
                    ["submitted_time"] = notification.SubmittedOn.ToString("HH:mm")
                }
            };

            var response = await emailService.SendEmailAsync(email, cancellationToken);

            if (response.Status == EmailStatus.Sent || response.Status == EmailStatus.Queued || response.Status == EmailStatus.Accepted)
            {
                _logger.LogInformation("Email sent successfully for submitted application {ApplicationId} (Reference: {ApplicationReference}) to {UserEmail}. Status: {EmailStatus}, Template: {TemplateId}",
                    notification.ApplicationId.Value, notification.ApplicationReference, notification.UserEmail, response.Status, emailTemplateId);
            }
            else
            {
                _logger.LogWarning("Failed to send email for submitted application {ApplicationId} (Reference: {ApplicationReference}) to {UserEmail}. Status: {EmailStatus}, Template: {TemplateId}",
                    notification.ApplicationId.Value, notification.ApplicationReference, notification.UserEmail, response.Status, emailTemplateId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email for submitted application {ApplicationId} (Reference: {ApplicationReference}) to {UserEmail}",
                notification.ApplicationId.Value, notification.ApplicationReference, notification.UserEmail);
            
            // Don't rethrow - email failures shouldn't break the application submission process
            // The application submission itself has already succeeded at this point
        }
    }
}
