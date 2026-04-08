using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Service for resolving email template IDs based on application template and email type.
/// Reads configuration from the current tenant's settings at runtime.
/// </summary>
public class EmailTemplateResolver(
    ITenantContextAccessor tenantContextAccessor,
    ILogger<EmailTemplateResolver> logger) : IEmailTemplateResolver
{
    public Task<string?> ResolveEmailTemplateAsync(TemplateId templateId, string emailType)
    {
        var (appTemplatesConfig, emailTemplatesConfig) = GetTenantConfigs();

        var applicationType = GetApplicationTypeByTemplateId(templateId.Value, appTemplatesConfig);

        if (string.IsNullOrEmpty(applicationType))
        {
            logger.LogWarning("Could not determine application type for template ID {TemplateId}", templateId.Value);
            return Task.FromResult<string?>(null);
        }

        var emailTemplateId = emailTemplatesConfig.GetTemplateId(applicationType, emailType);

        if (string.IsNullOrEmpty(emailTemplateId))
        {
            logger.LogWarning("Could not find email template for application type {ApplicationType} and email type {EmailType}",
                applicationType, emailType);
            return Task.FromResult<string?>(null);
        }

        logger.LogDebug("Resolved email template {TemplateId} for application type {ApplicationType} and email type {EmailType}",
            emailTemplateId, applicationType, emailType);

        return Task.FromResult<string?>(emailTemplateId);
    }

    public Task<string?> GetApplicationTypeAsync(TemplateId templateId)
    {
        var (appTemplatesConfig, _) = GetTenantConfigs();
        var applicationType = GetApplicationTypeByTemplateId(templateId.Value, appTemplatesConfig);
        return Task.FromResult(applicationType);
    }

    private (ApplicationTemplatesConfiguration appTemplates, EmailTemplatesConfiguration emailTemplates) GetTenantConfigs()
    {
        var tenant = tenantContextAccessor.CurrentTenant
            ?? throw new InvalidOperationException("No tenant context available for email template resolution.");

        var appTemplatesConfig = new ApplicationTemplatesConfiguration();
        tenant.Settings.GetSection("ApplicationTemplates").Bind(appTemplatesConfig);

        var emailTemplatesConfig = new EmailTemplatesConfiguration();
        tenant.Settings.GetSection("EmailTemplates").Bind(emailTemplatesConfig);

        return (appTemplatesConfig, emailTemplatesConfig);
    }

    private string? GetApplicationTypeByTemplateId(Guid templateId, ApplicationTemplatesConfiguration appTemplatesConfig)
    {
        var templateIdString = templateId.ToString();

        var mapping = appTemplatesConfig.HostMappings
            .FirstOrDefault(kvp => string.Equals(kvp.Value, templateIdString, StringComparison.OrdinalIgnoreCase));

        if (mapping.Key == null)
        {
            logger.LogWarning("Template ID {TemplateId} not found in host mappings", templateIdString);
            return null;
        }

        return ConvertHostMappingToApplicationType(mapping.Key);
    }

    private static string ConvertHostMappingToApplicationType(string hostMapping)
    {
        return hostMapping switch
        {
            "transfers" => "Transfers",
            "sigchange" => "SigChange",
            _ => string.Concat(hostMapping[0].ToString().ToUpper(), hostMapping.AsSpan(1))
        };
    }
}
