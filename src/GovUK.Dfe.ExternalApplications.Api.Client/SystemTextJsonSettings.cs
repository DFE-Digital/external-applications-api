using System.Text.Json;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.ExternalApplications.Api.Client
{
    public static class SystemTextJsonSettings
    {
        public static JsonSerializerOptions CreateSerializerSettings()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
            
            return options;
        }
    }
} 