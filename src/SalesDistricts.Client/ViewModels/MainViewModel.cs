using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SalesDistricts.Client.Api;

namespace SalesDistricts.Client.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IDistrictApiClient _api;

    public MainViewModel(IDistrictApiClient api, DistrictDetailViewModel detail)
    {
        _api = api;
        Detail = detail;
        Detail.DistrictUpdated += OnDistrictUpdated;
    }

    public DistrictDetailViewModel Detail { get; }

    public ObservableCollection<DistrictItemViewModel> Districts { get; } = [];

    [ObservableProperty]
    public partial DistrictItemViewModel? SelectedDistrict { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        ErrorMessage = null;
        try
        {
            var districts = await _api.GetDistrictsAsync(ct);
            Districts.Clear();
            foreach (var district in districts)
                Districts.Add(new DistrictItemViewModel(district.Id, district.Name, district.PrimarySalesperson));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = UserMessage.For(ex);
        }
    }

    partial void OnSelectedDistrictChanged(DistrictItemViewModel? value)
    {
        if (value is not null)
            Detail.LoadCommand.Execute(value.Id);
    }

    private void OnDistrictUpdated(DistrictDetailDto district)
    {
        if (Districts.FirstOrDefault(d => d.Id == district.Id) is { } row)
            row.PrimarySalesperson = district.Primary.Name;
    }
}
