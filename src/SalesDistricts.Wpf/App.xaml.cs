using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using SalesDistricts.Client.Api;
using SalesDistricts.Client.Services;
using SalesDistricts.Client.ViewModels;

namespace SalesDistricts.Wpf;

public partial class App : Application
{
    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            var baseUrl = context.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException("Api:BaseUrl is missing from appsettings.json.");

            services.AddHttpClient<IDistrictApiClient, DistrictApiClient>(client => client.BaseAddress = new Uri(baseUrl))
                // The ViewModels live as long as the app and keep their client, so recycle connections here.
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
                .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());   // retry GETs only

            services.AddSingleton<IDialogService, MessageBoxDialogService>();
            services.AddSingleton<DistrictDetailViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
