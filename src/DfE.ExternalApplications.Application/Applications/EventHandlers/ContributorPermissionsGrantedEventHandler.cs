using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Events;
using Microsoft.Extensions.Logging;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class ContributorPermissionsGrantedEventHandler(
    ILogger<ContributorPermissionsGrantedEventHandler> logger,
    IEmailService emailService,
    IEmailTemplateResolver emailTemplateResolver) : BaseEventHandler<ContributorPermissionsGrantedEvent>(logger)
{
    protected override async Task HandleEvent(ContributorPermissionsGrantedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling permissions granted for contributor {ContributorId} to application {ApplicationId} by {GrantedBy}",
            notification.Contributor.Id!.Value,
            notification.ApplicationId.Value,
            notification.GrantedBy.Value);

        logger.LogInformation("Successfully processed permissions granted for contributor {ContributorId} to application {ApplicationId}",
            notification.Contributor.Id!.Value,
            notification.ApplicationId.Value);

        // Send email to the contributor about their new access
        await SendContributorAccessGrantedEmail(notification, cancellationToken);
    }

    private async Task SendContributorAccessGrantedEmail(ContributorPermissionsGrantedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the email template ID based on the application template and email type
            var emailTemplateId = await emailTemplateResolver.ResolveEmailTemplateAsync(
                notification.TemplateId,
                "ContributorInvited"); // Reuse the same template for now, could be "ContributorAccessGranted" if you want different template

            if (string.IsNullOrEmpty(emailTemplateId))
            {
                logger.LogError("Could not resolve email template for contributor access granted to application {ApplicationId} with template {TemplateId}",
                    notification.ApplicationId.Value, notification.TemplateId.Value);
                return;
            }

            var email = new EmailMessage()
            {
                ToEmail = notification.Contributor.Email,
                TemplateId = emailTemplateId,
                Personalization = new Dictionary<string, object>
                {
                    ["contributor_name"] = notification.Contributor.Name,
                    ["application_reference"] = notification.ApplicationReference,
                    ["granted_date"] = notification.GrantedOn.ToString("dd/MM/yyyy"),
                    ["granted_time"] = notification.GrantedOn.ToString("HH:mm"),
                    ["access_types"] = string.Join(", ", notification.GrantedAccessTypes.Select(a => a.ToString()))
                }
            };

            var response = await emailService.SendEmailAsync(email, cancellationToken);

            if (response.Status == EmailStatus.Sent || response.Status == EmailStatus.Queued || response.Status == EmailStatus.Accepted)
            {
                logger.LogInformation("Contributor access granted email sent successfully for {ContributorEmail} for application {ApplicationId} (Reference: {ApplicationReference}). Status: {EmailStatus}, Template: {TemplateId}",
                    notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference, response.Status, emailTemplateId);
            }
            else
            {
                logger.LogWarning("Failed to send contributor access granted email for {ContributorEmail} for application {ApplicationId} (Reference: {ApplicationReference}). Status: {EmailStatus}, Template: {TemplateId}",
                    notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference, response.Status, emailTemplateId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending contributor access granted email for {ContributorEmail} for application {ApplicationId} (Reference: {ApplicationReference})",
                notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference);

            // Don't rethrow - email failures shouldn't break the permission granting process
        }
    }
}
