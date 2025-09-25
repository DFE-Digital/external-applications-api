namespace DfE.ExternalApplications.Application.Common.Models;

/// <summary>
/// Configuration for email templates organized by application type
/// </summary>
public class EmailTemplatesConfiguration
{
    /// <summary>
    /// Email templates organized by application type
    /// Key: Application type name (e.g., "Transfer", "SigChange")
    /// Value: Dictionary of email template types to template IDs
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Templates { get; set; } = new();

    /// <summary>
    /// Gets an email template ID for a specific application type and email type
    /// </summary>
    /// <param name="applicationType">The application type (e.g., "Transfer")</param>
    /// <param name="emailType">The email type (e.g., "ApplicationSubmitted")</param>
    /// <returns>The template ID if found, otherwise null</returns>
    public string? GetTemplateId(string applicationType, string emailType)
    {
        if (Templates.TryGetValue(applicationType, out var typeTemplates))
        {
            typeTemplates.TryGetValue(emailType, out var templateId);
            return templateId;
        }
        return null;
    }

    /// <summary>
    /// Gets all available email types for a specific application type
    /// </summary>
    /// <param name="applicationType">The application type</param>
    /// <returns>Collection of available email types</returns>
    public IEnumerable<string> GetAvailableEmailTypes(string applicationType)
    {
        return Templates.TryGetValue(applicationType, out var typeTemplates) 
            ? typeTemplates.Keys 
            : Enumerable.Empty<string>();
    }
}
