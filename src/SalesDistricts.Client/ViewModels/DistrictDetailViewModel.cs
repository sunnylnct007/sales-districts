using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SalesDistricts.Client.Api;
using SalesDistricts.Client.Services;

namespace SalesDistricts.Client.ViewModels;

public sealed partial class DistrictDetailViewModel(IDistrictApiClient api, IDialogService dialogs) : ObservableObject
{
    private IReadOnlyList<SalespersonDto> _allSalespeople = [];
    private CancellationTokenSource? _loadCancellation;

    // Raised after a successful change, so the district list can update its row.
    public event Action<DistrictDetailDto>? DistrictUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableSalespeople))]
    [NotifyCanExecuteChangedFor(nameof(AddSecondaryCommand), nameof(MakePrimaryCommand),
        nameof(RemoveSecondaryCommand), nameof(PromoteSecondaryCommand))]
    public partial DistrictDetailDto? District { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSecondaryCommand), nameof(MakePrimaryCommand))]
    public partial SalespersonDto? SelectedSalesperson { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSecondaryCommand), nameof(PromoteSecondaryCommand))]
    public partial SalespersonDto? SelectedSecondary { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // Everyone not yet in the district: the choices for "add as secondary" and "make primary".
    public IReadOnlyList<SalespersonDto> AvailableSalespeople => District is { } district
        ? [.. _allSalespeople.Where(s => s.Id != district.Primary.Id && district.Secondaries.All(x => x.Id != s.Id))]
        : [];

    // A newer selection cancels the previous load, so a slow response can't overwrite it.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadAsync(int districtId)
    {
        _loadCancellation?.Cancel();
        var cancellation = _loadCancellation = new CancellationTokenSource();
        ErrorMessage = null;

        try
        {
            if (_allSalespeople.Count == 0)
                _allSalespeople = await api.GetSalespeopleAsync(cancellation.Token);

            var district = await api.GetDistrictAsync(districtId, cancellation.Token);
            SelectedSalesperson = null;
            SelectedSecondary = null;
            District = district;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = UserMessage.For(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedSalesperson))]
    private Task AddSecondaryAsync(CancellationToken ct) =>
        District is { } district && SelectedSalesperson is { } person
            ? ChangeAsync(() => api.AddSecondaryAsync(district.Id, person.Id, ct))
            : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanUseSelectedSalesperson))]
    private Task MakePrimaryAsync(CancellationToken ct) =>
        SelectedSalesperson is { } person ? ReplacePrimaryAsync(person, ct) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanUseSelectedSecondary))]
    private Task RemoveSecondaryAsync(CancellationToken ct) =>
        District is { } district && SelectedSecondary is { } person
            ? ChangeAsync(() => api.RemoveSecondaryAsync(district.Id, person.Id, ct))
            : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanUseSelectedSecondary))]
    private Task PromoteSecondaryAsync(CancellationToken ct) =>
        SelectedSecondary is { } person ? ReplacePrimaryAsync(person, ct) : Task.CompletedTask;

    private bool CanUseSelectedSalesperson() => District is not null && SelectedSalesperson is not null;

    private bool CanUseSelectedSecondary() => District is not null && SelectedSecondary is not null;

    // The previous primary leaves the district, so the user confirms first.
    private Task ReplacePrimaryAsync(SalespersonDto newPrimary, CancellationToken ct)
    {
        if (District is not { } district)
            return Task.CompletedTask;

        var confirmed = dialogs.Confirm(
            $"{newPrimary.Name} becomes the primary of {district.Name}, and {district.Primary.Name} will be removed from the district. Continue?");

        return confirmed
            ? ChangeAsync(() => api.ReplacePrimaryAsync(district.Id, newPrimary.Id, ct))
            : Task.CompletedTask;
    }

    // The screen only changes once the server confirms; on error it keeps showing what the server last said.
    private async Task ChangeAsync(Func<Task<DistrictDetailDto>> change)
    {
        ErrorMessage = null;
        try
        {
            var updated = await change();
            SelectedSalesperson = null;
            SelectedSecondary = null;
            District = updated;
            DistrictUpdated?.Invoke(updated);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = UserMessage.For(ex);
        }
    }
}
