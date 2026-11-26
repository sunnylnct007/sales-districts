using System.Net;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SalesDistricts.Client.Api;
using SalesDistricts.Client.Services;
using SalesDistricts.Client.ViewModels;

namespace SalesDistricts.Client.Tests;

public sealed class ViewModelTests
{
    private static readonly SalespersonDto Anna = new(1, "Anna Jensen"), Bo = new(2, "Bo Nielsen"),
        Carl = new(3, "Carl Hansen"), Gustav = new(7, "Gustav Madsen");

    // Capital Region: Carl is primary, Bo is secondary.
    private static readonly DistrictDetailDto CapitalRegion =
        new(5, "Capital Region", Carl, [Bo], [new StoreDto(9, "Copenhagen Store")]);

    private readonly IDistrictApiClient _api = Substitute.For<IDistrictApiClient>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly MainViewModel _main;

    public ViewModelTests()
    {
        _api.GetDistrictsAsync(Arg.Any<CancellationToken>())
            .Returns([new DistrictSummaryDto(5, "Capital Region", "Carl Hansen")]);
        _api.GetSalespeopleAsync(Arg.Any<CancellationToken>()).Returns([Anna, Bo, Carl, Gustav]);
        _api.GetDistrictAsync(5, Arg.Any<CancellationToken>()).Returns(CapitalRegion);

        _main = new MainViewModel(_api, new DistrictDetailViewModel(_api, _dialogs));
    }

    private DistrictDetailViewModel Detail => _main.Detail;

    private async Task SelectCapitalRegionAsync()
    {
        await _main.LoadCommand.ExecuteAsync(null);
        _main.SelectedDistrict = _main.Districts.Single();
        await Detail.LoadCommand.ExecutionTask!;
    }

    [Fact]
    public async Task Loading_fills_the_district_list()
    {
        await _main.LoadCommand.ExecuteAsync(null);

        var row = Assert.Single(_main.Districts);
        Assert.Equal(("Capital Region", "Carl Hansen"), (row.Name, row.PrimarySalesperson));
    }

    [Fact]
    public async Task Selecting_a_district_loads_its_detail_and_the_choices()
    {
        await SelectCapitalRegionAsync();

        Assert.Same(CapitalRegion, Detail.District);
        Assert.Equal([Anna, Gustav], Detail.AvailableSalespeople);   // Carl and Bo are already in the district
    }

    [Fact]
    public async Task Change_buttons_are_disabled_until_someone_is_selected()
    {
        await SelectCapitalRegionAsync();

        Assert.False(Detail.AddSecondaryCommand.CanExecute(null));
        Assert.False(Detail.RemoveSecondaryCommand.CanExecute(null));

        Detail.SelectedSalesperson = Gustav;
        Assert.True(Detail.AddSecondaryCommand.CanExecute(null));
    }

    [Fact]
    public async Task Adding_a_secondary_shows_the_updated_district()
    {
        var updated = CapitalRegion with { Secondaries = [Bo, Gustav] };
        _api.AddSecondaryAsync(5, Gustav.Id, Arg.Any<CancellationToken>()).Returns(updated);
        await SelectCapitalRegionAsync();

        Detail.SelectedSalesperson = Gustav;
        await Detail.AddSecondaryCommand.ExecuteAsync(null);

        Assert.Same(updated, Detail.District);
        Assert.Null(Detail.ErrorMessage);
    }

    [Fact]
    public async Task A_rejected_change_shows_the_reason_and_keeps_the_district()
    {
        _api.AddSecondaryAsync(5, Gustav.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "Salesperson is already a secondary in this district."));
        await SelectCapitalRegionAsync();

        Detail.SelectedSalesperson = Gustav;
        await Detail.AddSecondaryCommand.ExecuteAsync(null);

        Assert.Equal("Salesperson is already a secondary in this district.", Detail.ErrorMessage);
        Assert.Same(CapitalRegion, Detail.District);
    }

    [Fact]
    public async Task Replacing_the_primary_does_nothing_if_the_user_cancels()
    {
        _dialogs.Confirm(Arg.Any<string>()).Returns(false);
        await SelectCapitalRegionAsync();

        Detail.SelectedSalesperson = Gustav;
        await Detail.MakePrimaryCommand.ExecuteAsync(null);

        await _api.DidNotReceive().ReplacePrimaryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Promoting_a_secondary_updates_the_district_list_row()
    {
        _dialogs.Confirm(Arg.Any<string>()).Returns(true);
        _api.ReplacePrimaryAsync(5, Bo.Id, Arg.Any<CancellationToken>())
            .Returns(CapitalRegion with { Primary = Bo, Secondaries = [] });
        await SelectCapitalRegionAsync();

        Detail.SelectedSecondary = Bo;
        await Detail.PromoteSecondaryCommand.ExecuteAsync(null);

        Assert.Equal("Bo Nielsen", _main.Districts.Single().PrimarySalesperson);
    }

    [Fact]
    public async Task An_unreachable_server_shows_a_friendly_message()
    {
        _api.GetDistrictsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException());

        await _main.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Can't reach the server. Check your connection and try again.", _main.ErrorMessage);
    }
}
