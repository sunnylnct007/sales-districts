using System.Net;
using System.Text;

namespace SalesDistricts.Client.Tests;

// Returns a canned response and remembers the request, so tests need no real server.
public sealed class StubHttpHandler(HttpStatusCode status, string body = "", string mediaType = "application/json") : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Request = request;
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
    }
}
