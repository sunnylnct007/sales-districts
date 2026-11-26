using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SalesDistricts.Client.Api;

// The API answered with an error. Message is safe to show to the user.
public sealed class ApiException(HttpStatusCode statusCode, string message, string? traceId = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? TraceId { get; } = traceId;

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var problem = await TryReadProblemAsync(response, ct).ConfigureAwait(false);

        var message = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault()
                      ?? problem?.Detail
                      ?? problem?.Title
                      ?? $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";

        return new ApiException(response.StatusCode, message, problem?.TraceId);
    }

    private static async Task<ProblemDto?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDto>(ct).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;   // empty or non-JSON body, e.g. a plain 401
        }
    }
}
