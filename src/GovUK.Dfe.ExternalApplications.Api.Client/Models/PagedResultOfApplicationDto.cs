using System.Text.Json.Serialization;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Contracts;

public partial class PagedResultOfApplicationDto
{
    [JsonPropertyName("items")]
    public IReadOnlyCollection<ApplicationDto> Items { get; init; } = Array.Empty<ApplicationDto>();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}
