using SalesDistricts.Client.Api;

namespace SalesDistricts.Client.ViewModels;

public static class UserMessage
{
    public static string For(Exception exception) => exception switch
    {
        ApiException { TraceId: { } traceId } api => $"{api.Message} (ref {traceId})",
        ApiException api => api.Message,
        HttpRequestException => "Can't reach the server. Check your connection and try again.",
        _ => "Something went wrong. Please try again."
    };
}
