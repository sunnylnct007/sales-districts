using System.Net.Http.Json;

namespace SalesDistricts.Client.Api;

public interface IDistrictApiClient
{
    Task<IReadOnlyList<DistrictSummaryDto>> GetDistrictsAsync(CancellationToken ct);
    Task<DistrictDetailDto> GetDistrictAsync(int districtId, CancellationToken ct);
    Task<IReadOnlyList<SalespersonDto>> GetSalespeopleAsync(CancellationToken ct);
    Task<DistrictDetailDto> AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task<DistrictDetailDto> RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task<DistrictDetailDto> ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct);
}

// Typed HttpClient: the HttpClient (base address, handlers) is configured and supplied by IHttpClientFactory.
public sealed class DistrictApiClient(HttpClient http) : IDistrictApiClient
{
    public Task<IReadOnlyList<DistrictSummaryDto>> GetDistrictsAsync(CancellationToken ct) =>
        SendAsync<IReadOnlyList<DistrictSummaryDto>>(HttpMethod.Get, "api/districts", body: null, ct);

    public Task<DistrictDetailDto> GetDistrictAsync(int districtId, CancellationToken ct) =>
        SendAsync<DistrictDetailDto>(HttpMethod.Get, $"api/districts/{districtId}", body: null, ct);

    public Task<IReadOnlyList<SalespersonDto>> GetSalespeopleAsync(CancellationToken ct) =>
        SendAsync<IReadOnlyList<SalespersonDto>>(HttpMethod.Get, "api/salespeople", body: null, ct);

    public Task<DistrictDetailDto> AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct) =>
        SendAsync<DistrictDetailDto>(HttpMethod.Post, $"api/districts/{districtId}/secondaries", new { salespersonId }, ct);

    public Task<DistrictDetailDto> RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct) =>
        SendAsync<DistrictDetailDto>(HttpMethod.Delete, $"api/districts/{districtId}/secondaries/{salespersonId}", body: null, ct);

    public Task<DistrictDetailDto> ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct) =>
        SendAsync<DistrictDetailDto>(HttpMethod.Put, $"api/districts/{districtId}/primary", new { salespersonId }, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct).ConfigureAwait(false);

        return await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false)
               ?? throw new ApiException(response.StatusCode, "The server returned an empty response.");
    }
}
