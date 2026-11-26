using NSubstitute;
using SalesDistricts.Api.Contracts;
using SalesDistricts.Api.Data;
using SalesDistricts.Api.Services;

namespace SalesDistricts.Api.UnitTests;

public sealed class DistrictServiceTests
{
    private const int CapitalRegion = 5, Carl = 3, Bo = 2, Gustav = 7, Unknown = 999;

    private readonly IDistrictRepository _districts = Substitute.For<IDistrictRepository>();
    private readonly ISalespersonRepository _salespeople = Substitute.For<ISalespersonRepository>();
    private readonly DistrictService _service;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DistrictServiceTests()
    {
        _service = new DistrictService(_districts, _salespeople);

        // Capital Region: Carl is primary, Bo is secondary. Gustav exists but has no district.
        _districts.GetDetailAsync(CapitalRegion, Arg.Any<CancellationToken>()).Returns(new DistrictDetail(
            CapitalRegion, "Capital Region",
            new SalespersonRecord(Carl, "Carl Hansen"),
            [new SalespersonRecord(Bo, "Bo Nielsen")],
            [new StoreRecord(9, "Copenhagen Store")]));
        _salespeople.ExistsAsync(Arg.Is<int>(id => id != Unknown), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task GetDistrictsAsync_maps_each_district_to_a_dto()
    {
        _districts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
            [new DistrictSummary(CapitalRegion, "Capital Region", "Carl Hansen")]);

        var districts = await _service.GetDistrictsAsync(Ct);

        Assert.Equal([new DistrictSummaryDto(CapitalRegion, "Capital Region", "Carl Hansen")], districts);
    }

    [Fact]
    public async Task GetDistrictAsync_maps_primary_secondaries_and_stores()
    {
        var district = await _service.GetDistrictAsync(CapitalRegion, Ct);

        Assert.NotNull(district);
        Assert.Equal(new SalespersonDto(Carl, "Carl Hansen"), district.Primary);
        Assert.Equal([new SalespersonDto(Bo, "Bo Nielsen")], district.Secondaries);
        Assert.Equal([new StoreDto(9, "Copenhagen Store")], district.Stores);
    }

    [Fact]
    public async Task GetDistrictAsync_returns_null_for_an_unknown_district()
    {
        Assert.Null(await _service.GetDistrictAsync(Unknown, Ct));
    }

    [Fact]
    public async Task AddSecondaryAsync_adds_a_salesperson_without_a_role()
    {
        var result = await _service.AddSecondaryAsync(CapitalRegion, Gustav, Ct);

        Assert.IsType<AssignmentResult.Success>(result);
        await _districts.Received(1).AddSecondaryAsync(CapitalRegion, Gustav, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(Carl, "already the primary")]
    [InlineData(Bo, "already a secondary")]
    public async Task AddSecondaryAsync_rejects_someone_already_in_the_district(int salespersonId, string reason)
    {
        var result = await _service.AddSecondaryAsync(CapitalRegion, salespersonId, Ct);

        var conflict = Assert.IsType<AssignmentResult.Conflict>(result);
        Assert.Contains(reason, conflict.Reason);
        await _districts.DidNotReceive().AddSecondaryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddSecondaryAsync_rejects_an_unknown_district_or_salesperson()
    {
        Assert.IsType<AssignmentResult.NotFound>(await _service.AddSecondaryAsync(Unknown, Gustav, Ct));
        Assert.IsType<AssignmentResult.Invalid>(await _service.AddSecondaryAsync(CapitalRegion, Unknown, Ct));
    }

    [Fact]
    public async Task AddSecondaryAsync_reports_not_found_if_the_district_disappears_after_the_change()
    {
        var capitalRegion = new DistrictDetail(
            CapitalRegion, "Capital Region", new SalespersonRecord(Carl, "Carl Hansen"), [], []);
        _districts.GetDetailAsync(CapitalRegion, Arg.Any<CancellationToken>())
            .Returns(capitalRegion, (DistrictDetail?)null);   // found before the change, gone after it

        var result = await _service.AddSecondaryAsync(CapitalRegion, Gustav, Ct);

        Assert.IsType<AssignmentResult.NotFound>(result);
    }

    [Fact]
    public async Task RemoveSecondaryAsync_removes_a_secondary()
    {
        var result = await _service.RemoveSecondaryAsync(CapitalRegion, Bo, Ct);

        Assert.IsType<AssignmentResult.Success>(result);
        await _districts.Received(1).RemoveSecondaryAsync(CapitalRegion, Bo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSecondaryAsync_cannot_remove_the_primary()
    {
        var result = await _service.RemoveSecondaryAsync(CapitalRegion, Carl, Ct);

        Assert.IsType<AssignmentResult.NotFound>(result);
        await _districts.DidNotReceive().RemoveSecondaryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplacePrimaryAsync_replaces_the_primary()
    {
        var result = await _service.ReplacePrimaryAsync(CapitalRegion, Gustav, Ct);

        Assert.IsType<AssignmentResult.Success>(result);
        await _districts.Received(1).ReplacePrimaryAsync(CapitalRegion, Gustav, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplacePrimaryAsync_promotes_a_secondary()
    {
        var result = await _service.ReplacePrimaryAsync(CapitalRegion, Bo, Ct);

        Assert.IsType<AssignmentResult.Success>(result);
        await _districts.Received(1).ReplacePrimaryAsync(CapitalRegion, Bo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplacePrimaryAsync_with_the_current_primary_changes_nothing()
    {
        var result = await _service.ReplacePrimaryAsync(CapitalRegion, Carl, Ct);

        var success = Assert.IsType<AssignmentResult.Success>(result);
        Assert.Equal(Carl, success.District.Primary.Id);
        await _districts.DidNotReceive().ReplacePrimaryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplacePrimaryAsync_rejects_an_unknown_district_or_salesperson()
    {
        Assert.IsType<AssignmentResult.NotFound>(await _service.ReplacePrimaryAsync(Unknown, Gustav, Ct));
        Assert.IsType<AssignmentResult.Invalid>(await _service.ReplacePrimaryAsync(CapitalRegion, Unknown, Ct));
    }
}
