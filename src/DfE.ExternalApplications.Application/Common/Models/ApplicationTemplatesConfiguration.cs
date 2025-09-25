namespace DfE.ExternalApplications.Application.Common.Models;

/// <summary>
/// Configuration for mapping application template names to their GUIDs
/// </summary>
public class ApplicationTemplatesConfiguration
{
    /// <summary>
    /// Maps host-friendly names to template GUIDs
    /// E.g., "transfer" -> "9A4E9C58-9135-468C-B154-7B966F7ACFB7"
    /// </summary>
    public Dictionary<string, string> HostMappings { get; set; } = new();
}
