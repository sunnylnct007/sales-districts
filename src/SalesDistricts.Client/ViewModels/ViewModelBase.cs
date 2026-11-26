using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SalesDistricts.Client.Api;

namespace SalesDistricts.Client.ViewModels;

public abstract partial class ViewModelBase(ILogger logger) : ObservableObject
{
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // The UI boundary: show the user a message for any failure, log the unexpected ones,
    // and ignore cancellations (e.g. a newer selection replacing a load).
    protected async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (ex is not ApiException and not HttpRequestException)
                logger.LogError(ex, "Unexpected error in {ViewModel}", GetType().Name);

            ErrorMessage = UserMessage.For(ex);
        }
    }
}
