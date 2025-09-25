using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Service for resolving email template IDs based on application template and email type
/// </summary>
public class EmailTemplateResolver : IEmailTemplateResolver
{
    private readonly ApplicationTemplatesConfiguration _appTemplatesConfig;
    private readonly EmailTemplatesConfiguration _emailTemplatesConfig;
    private readonly ILogger<EmailTemplateResolver> _logger;

    public EmailTemplateResolver(
        IOptions<ApplicationTemplatesConfiguration> appTemplatesOptions,
        IOptions<EmailTemplatesConfiguration> emailTemplatesOptions,
        ILogger<EmailTemplateResolver> logger)
    {
        _appTemplatesConfig = appTemplatesOptions.Value;
        _emailTemplatesConfig = emailTemplatesOptions.Value;
        _logger = logger;
    }

    public Task<string?> ResolveEmailTemplateAsync(TemplateId templateId, string emailType)
    {
        var applicationType = GetApplicationTypeByTemplateId(templateId.Value);
        
        if (string.IsNullOrEmpty(applicationType))
        {
            _logger.LogWarning("Could not determine application type for template ID {TemplateId}", templateId.Value);
            return Task.FromResult<string?>(null);
        }

        var emailTemplateId = _emailTemplatesConfig.GetTemplateId(applicationType, emailType);
        
        if (string.IsNullOrEmpty(emailTemplateId))
        {
            _logger.LogWarning("Could not find email template for application type {ApplicationType} and email type {EmailType}", 
                applicationType, emailType);
            return Task.FromResult<string?>(null);
        }

        _logger.LogDebug("Resolved email template {TemplateId} for application type {ApplicationType} and email type {EmailType}", 
            emailTemplateId, applicationType, emailType);

        return Task.FromResult<string?>(emailTemplateId);
    }

    public Task<string?> GetApplicationTypeAsync(TemplateId templateId)
    {
        var applicationType = GetApplicationTypeByTemplateId(templateId.Value);
        return Task.FromResult(applicationType);
    }

    private string? GetApplicationTypeByTemplateId(Guid templateId)
    {
        var templateIdString = templateId.ToString();
        
        // Find the application type by looking through the host mappings
        var mapping = _appTemplatesConfig.HostMappings
            .FirstOrDefault(kvp => string.Equals(kvp.Value, templateIdString, StringComparison.OrdinalIgnoreCase));

        if (mapping.Key == null)
        {
            _logger.LogWarning("Template ID {TemplateId} not found in host mappings", templateIdString);
            return null;
        }

        // Convert host mapping key to application type name
        // E.g., "transfer" -> "Transfer", "sigchange" -> "SigChange"
        return ConvertHostMappingToApplicationType(mapping.Key);
    }

    private static string ConvertHostMappingToApplicationType(string hostMapping)
    {
        return hostMapping switch
        {
            "transfer" => "Transfer",
            "sigchange" => "SigChange",
            _ => string.Concat(hostMapping[0].ToString().ToUpper(), hostMapping.AsSpan(1))
        };
    }
}
