using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Playlists;

public sealed class PlaylistSimulateEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PlaylistSimulateEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SimulateFromBookingRequest_ReturnsSimulation()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 750);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait");
        await CreateScreenAsync(client, building.Id, orientation: "Landscape");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id);

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            bookingRequest.Id,
            null,
            null,
            new DateOnly(2026, 7, 10),
            null,
            15,
            16,
            null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Equal(1, simulation!.EligibleScreens);
        Assert.Equal(1, simulation.EligibleBuildings);
        Assert.Equal(15, simulation.LoopDurationSeconds);
        Assert.Equal(3840d, simulation.EstimatedLoopsPerDay);
        Assert.Equal(3840d, simulation.EstimatedPlaysPerCreative);
        Assert.Equal(3840, simulation.EstimatedTotalPlays);
        Assert.Equal(750, simulation.EstimatedAudience);
        Assert.Single(simulation.Items);
        Assert.Equal("BookingRequest", simulation.Items[0].Source);
    }

    [Fact]
    public async Task SimulateFromCampaign_ReturnsSimulation()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var matchingBuilding = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Residential", audience: 900);
        var nonMatchingBuilding = await CreateBuildingAsync(client, city: "Porto", buildingType: "Residential", audience: 500);
        await CreateScreenAsync(client, matchingBuilding.Id, orientation: "Portrait");
        await CreateScreenAsync(client, nonMatchingBuilding.Id, orientation: "Portrait");

        await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/delivery-constraints", new UpsertDeliveryConstraintsRequest(
            ["Lisbon"],
            ["Residential"],
            ["Portrait"],
            ["Friday"],
            null,
            null));

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            campaign.Id,
            null,
            new DateOnly(2026, 6, 5),
            null,
            20,
            12,
            null));

        response.EnsureSuccessStatusCode();

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Equal(1, simulation!.EligibleScreens);
        Assert.Equal(1, simulation.EligibleBuildings);
        Assert.Equal(2160d, simulation.EstimatedLoopsPerDay);
        Assert.Equal("Campaign", simulation.Items[0].Source);
    }

    [Fact]
    public async Task SimulateFromInventoryPackage_ReturnsSimulation()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var matchingBuilding = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 1100);
        var wrongBuilding = await CreateBuildingAsync(client, city: "Porto", buildingType: "Corporate", audience: 600);
        await CreateScreenAsync(client, matchingBuilding.Id, orientation: "Portrait");
        await CreateScreenAsync(client, wrongBuilding.Id, orientation: "Portrait");
        var inventoryPackage = await CreateInventoryPackageAsync(client);

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            null,
            inventoryPackage.Id,
            new DateOnly(2026, 6, 5),
            null,
            30,
            10,
            null));

        response.EnsureSuccessStatusCode();

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Equal(1, simulation!.EligibleScreens);
        Assert.Equal(1, simulation.EligibleBuildings);
        Assert.Equal("InventoryPackage", simulation.Items[0].Source);
    }

    [Fact]
    public async Task Simulate_CalculatesLoopDuration()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var building = await CreateBuildingAsync(client);
        await CreateScreenAsync(client, building.Id);

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            null,
            null,
            new DateOnly(2026, 6, 5),
            null,
            22,
            8,
            null));

        response.EnsureSuccessStatusCode();

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Equal(22, simulation!.LoopDurationSeconds);
    }

    [Fact]
    public async Task Simulate_CalculatesEstimatedLoopsPerDay()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var building = await CreateBuildingAsync(client);
        await CreateScreenAsync(client, building.Id);

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            null,
            null,
            new DateOnly(2026, 6, 5),
            null,
            45,
            9,
            null));

        response.EnsureSuccessStatusCode();

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Equal(720d, simulation!.EstimatedLoopsPerDay);
    }

    [Fact]
    public async Task Simulate_ReturnsWarning_WhenNoCampaignConstraints()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var building = await CreateBuildingAsync(client);
        await CreateScreenAsync(client, building.Id);

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            campaign.Id,
            null,
            new DateOnly(2026, 6, 5),
            null,
            15,
            8,
            null));

        response.EnsureSuccessStatusCode();

        var simulation = await response.Content.ReadFromJsonAsync<PlaylistSimulateResponse>();
        Assert.NotNull(simulation);
        Assert.Contains(simulation!.Warnings, item => item.Contains("No delivery constraints found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Simulate_ReturnsBadRequest_WhenInvalidCreativeDuration()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            null,
            null,
            new DateOnly(2026, 6, 5),
            null,
            0,
            8,
            null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Simulate_ReturnsBadRequest_WhenInvalidOperatingHours()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/playlists/simulate", new PlaylistSimulateRequest(
            null,
            null,
            null,
            new DateOnly(2026, 6, 5),
            null,
            15,
            0,
            null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Simulate_ReturnsBadRequest_WhenDateIsInvalid()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        using var response = await client.PostAsync(
            "/api/playlists/simulate",
            JsonContent.Create(new
            {
                bookingRequestId = (Guid?)null,
                campaignId = (Guid?)null,
                inventoryPackageId = (Guid?)null,
                date = "not-a-date",
                screenIds = (Guid[]?)null,
                creativeDurationSeconds = 15,
                operatingHoursPerDay = 8,
                maxLoopDurationSeconds = (int?)null
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private static async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/advertisers", new CreateAdvertiserRequest(
            "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active"));
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private static async Task<BookingRequestDto> CreateBookingRequestAsync(HttpClient client, Guid advertiserId)
    {
        var response = await client.PostAsJsonAsync("/api/booking-requests", new CreateBookingRequestRequest(
            advertiserId,
            "Lisbon Summer Launch",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            15,
            500m,
            "Brand awareness",
            "Target premium office buildings"));
        response.EnsureSuccessStatusCode();

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        return bookingRequest!;
    }

    private static async Task<CampaignDto> CreateCampaignAsync(HttpClient client)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var response = await client.PostAsJsonAsync("/api/campaigns", new CreateCampaignRequest(
            advertiser.Id,
            "Summer Push",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            "Draft",
            100m,
            1000m,
            8.5m));
        response.EnsureSuccessStatusCode();

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        return campaign!;
    }

    private static async Task<InventoryPackageDto> CreateInventoryPackageAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/inventory-packages", new CreateInventoryPackageRequest(
            "Lisbon Corporate Buildings",
            "Premium office inventory",
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            [],
            [],
            12.5m,
            "Active"));
        response.EnsureSuccessStatusCode();

        var inventoryPackage = await response.Content.ReadFromJsonAsync<InventoryPackageDto>();
        Assert.NotNull(inventoryPackage);
        return inventoryPackage!;
    }

    private static async Task<BuildingDto> CreateBuildingAsync(
        HttpClient client,
        string city = "Lisbon",
        string buildingType = "Corporate",
        int audience = 1200)
    {
        var response = await client.PostAsJsonAsync("/api/buildings", new CreateBuildingRequest(
            $"Building-{Guid.NewGuid():N}",
            "123 Main St",
            city,
            "Portugal",
            "1000-001",
            buildingType,
            audience));
        response.EnsureSuccessStatusCode();

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        return building!;
    }

    private static async Task<ScreenDto> CreateScreenAsync(
        HttpClient client,
        Guid buildingId,
        string orientation = "Portrait")
    {
        var response = await client.PostAsJsonAsync("/api/screens", new CreateScreenRequest(
            buildingId,
            $"Screen-{Guid.NewGuid():N}",
            $"SCR-{Guid.NewGuid():N}",
            1080,
            1920,
            orientation,
            "Active"));
        response.EnsureSuccessStatusCode();

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        return screen!;
    }

    private sealed record PlaylistSimulateRequest(
        Guid? BookingRequestId,
        Guid? CampaignId,
        Guid? InventoryPackageId,
        DateOnly Date,
        List<Guid>? ScreenIds,
        int CreativeDurationSeconds,
        double OperatingHoursPerDay,
        int? MaxLoopDurationSeconds);

    private sealed record PlaylistSimulateResponse(
        DateOnly Date,
        int EligibleScreens,
        int EligibleBuildings,
        int LoopDurationSeconds,
        double EstimatedLoopsPerDay,
        double EstimatedPlaysPerCreative,
        long EstimatedTotalPlays,
        long EstimatedAudience,
        List<PlaylistSimulateItem> Items,
        List<string> Warnings,
        List<string> Conflicts);

    private sealed record PlaylistSimulateItem(
        int Order,
        Guid? CampaignId,
        Guid? CreativeId,
        int CreativeDurationSeconds,
        string Source,
        string? Notes);

    private sealed record CreateAdvertiserRequest(
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status);

    private sealed record AdvertiserDto(Guid Id);

    private sealed record CreateBookingRequestRequest(
        Guid AdvertiserId,
        string Name,
        DateTime DateFrom,
        DateTime DateTo,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        int CreativeDurationSeconds,
        decimal Budget,
        string CampaignObjective,
        string Notes);

    private sealed record BookingRequestDto(Guid Id);

    private sealed record CreateCampaignRequest(
        Guid AdvertiserId,
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        string Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm);

    private sealed record CampaignDto(Guid Id);

    private sealed record UpsertDeliveryConstraintsRequest(
        IReadOnlyList<string> Cities,
        IReadOnlyList<string> BuildingTypes,
        IReadOnlyList<string> ScreenOrientations,
        IReadOnlyList<string> DaysOfWeek,
        TimeOnly? StartTime,
        TimeOnly? EndTime);

    private sealed record CreateInventoryPackageRequest(
        string Name,
        string Description,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        List<Guid> ScreenIds,
        List<Guid> BuildingIds,
        decimal BaseCpm,
        string Status);

    private sealed record InventoryPackageDto(Guid Id);

    private sealed record CreateBuildingRequest(
        string Name,
        string Address,
        string City,
        string Country,
        string PostalCode,
        string BuildingType,
        int EstimatedDailyAudience);

    private sealed record BuildingDto(Guid Id);

    private sealed record CreateScreenRequest(
        Guid BuildingId,
        string Name,
        string ExternalCode,
        int ResolutionWidth,
        int ResolutionHeight,
        string Orientation,
        string Status);

    private sealed record ScreenDto(Guid Id);
}
