using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class ContributorAddedEventHandler(
    ILogger<ContributorAddedEventHandler> logger,
    IEmailService emailService,
    IEmailTemplateResolver emailTemplateResolver) : BaseEventHandler<ContributorAddedEvent>(logger)
{
    protected override async Task HandleEvent(ContributorAddedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing contributor added event for {ContributorId} to application {ApplicationId} by {AddedBy}", 
            notification.Contributor.Id!.Value, 
            notification.ApplicationId.Value, 
            notification.AddedBy.Value);

        // Send email to the new contributor (side effect only)
        await SendContributorInvitationEmail(notification, cancellationToken);
    }

    private async Task SendContributorInvitationEmail(ContributorAddedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
                // Resolve the email template ID based on the application template and email type
                var emailTemplateId = await emailTemplateResolver.ResolveEmailTemplateAsync(
                    notification.TemplateId,
                    "ContributorInvited");

            if (string.IsNullOrEmpty(emailTemplateId))
            {
                logger.LogError("Could not resolve email template for contributor invitation to application {ApplicationId} with template {TemplateId}",
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
                    ["added_date"] = notification.AddedOn.ToString("dd/MM/yyyy"),
                    ["added_time"] = notification.AddedOn.ToString("HH:mm")
                }
            };

            var response = await emailService.SendEmailAsync(email, cancellationToken);

            if (response.Status == EmailStatus.Sent || response.Status == EmailStatus.Queued || response.Status == EmailStatus.Accepted)
            {
                logger.LogInformation("Contributor invitation email sent successfully for {ContributorEmail} added to application {ApplicationId} (Reference: {ApplicationReference}). Status: {EmailStatus}, Template: {TemplateId}",
                    notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference, response.Status, emailTemplateId);
            }
            else
            {
                logger.LogWarning("Failed to send contributor invitation email for {ContributorEmail} added to application {ApplicationId} (Reference: {ApplicationReference}). Status: {EmailStatus}, Template: {TemplateId}",
                    notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference, response.Status, emailTemplateId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending contributor invitation email for {ContributorEmail} added to application {ApplicationId} (Reference: {ApplicationReference})",
                notification.Contributor.Email, notification.ApplicationId.Value, notification.ApplicationReference);
            
            // Don't rethrow - email failures shouldn't break the contributor addition process
            // The contributor addition itself has already succeeded at this point
        }
    }
} 