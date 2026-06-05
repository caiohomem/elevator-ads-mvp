using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Programmatic;

public sealed class SimulatorForecastEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task PostForecast_WithValidRequest_ReturnsForecast()
    {
        var client = CreateClient();
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 600);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait", status: "Active");

        var response = await client.PostAsJsonAsync("/api/programmatic/simulator/forecast", new SimulatorForecastRequest(
            "buyer-1",
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 7),
            ["lisbon"],
            ["corporate"],
            ["portrait"],
            15,
            500m,
            "Awareness",
            "Test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<SimulatorForecastResponse>();
        Assert.NotNull(forecast);
        Assert.Equal(1, forecast!.EligibleScreens);
        Assert.Equal(1, forecast.EligibleBuildings);
        Assert.Equal(96, forecast.EstimatedPlays);
        Assert.Equal(1800, forecast.EstimatedAudience);
        Assert.Equal(0.96m, forecast.EstimatedCost);
        Assert.Equal(1.0m, forecast.AvailableCapacity);
        Assert.Contains(forecast.Warnings, item => item.Contains("placeholder CPM", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Contact sales to convert this forecast into a scheduled playlist campaign.", forecast.SuggestedNextAction);
    }

    [Fact]
    public async Task PostForecast_AppliesFilters_ToReduceEligibleScreens()
    {
        var client = CreateClient();
        var lisbon = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 800);
        var porto = await CreateBuildingAsync(client, city: "Porto", buildingType: "Residential", audience: 500);
        await CreateScreenAsync(client, lisbon.Id, orientation: "Portrait", status: "Active");
        await CreateScreenAsync(client, porto.Id, orientation: "Landscape", status: "Active");

        var response = await client.PostAsJsonAsync("/api/programmatic/simulator/forecast", new SimulatorForecastRequest(
            null,
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 5),
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            30,
            null,
            null,
            null));

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<SimulatorForecastResponse>();
        Assert.NotNull(forecast);
        Assert.Equal(1, forecast!.EligibleScreens);
        Assert.Equal(1, forecast.EligibleBuildings);
    }

    [Fact]
    public async Task PostForecast_WithInvalidDateRange_ReturnsBadRequest()
    {
        var client = new TestWebApplicationFactory().CreateClient();

        var response = await client.PostAsJsonAsync("/api/programmatic/simulator/forecast", new SimulatorForecastRequest(
            null,
            new DateOnly(2026, 6, 8),
            new DateOnly(2026, 6, 5),
            null,
            null,
            null,
            15,
            null,
            null,
            null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostForecast_WithInvalidCreativeDuration_ReturnsBadRequest()
    {
        var client = new TestWebApplicationFactory().CreateClient();

        var response = await client.PostAsJsonAsync("/api/programmatic/simulator/forecast", new SimulatorForecastRequest(
            null,
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 6),
            null,
            null,
            null,
            0,
            null,
            null,
            null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostForecast_WhenAudienceDataMissing_ReturnsWarning()
    {
        var client = CreateClient();
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Commercial", audience: 0);
        await CreateScreenAsync(client, building.Id, orientation: "Landscape", status: "Active");

        var response = await client.PostAsJsonAsync("/api/programmatic/simulator/forecast", new SimulatorForecastRequest(
            null,
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 6),
            null,
            null,
            null,
            20,
            null,
            null,
            null));

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<SimulatorForecastResponse>();
        Assert.NotNull(forecast);
        Assert.Contains(forecast!.Warnings, item => item.Contains("missing audience data", StringComparison.OrdinalIgnoreCase));
    }

    private static HttpClient CreateClient() => new TestWebApplicationFactory().CreateAuthenticatedClient();

    private static async Task<BuildingDto> CreateBuildingAsync(
        HttpClient client,
        string city,
        string buildingType,
        int audience)
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
        string orientation,
        string status)
    {
        var response = await client.PostAsJsonAsync("/api/screens", new CreateScreenRequest(
            buildingId,
            $"Screen-{Guid.NewGuid():N}",
            $"SCR-{Guid.NewGuid():N}",
            1080,
            1920,
            orientation,
            status));
        response.EnsureSuccessStatusCode();

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        return screen!;
    }

    private sealed record SimulatorForecastRequest(
        string? AdvertiserId,
        DateOnly DateFrom,
        DateOnly DateTo,
        List<string>? Cities,
        List<string>? BuildingTypes,
        List<string>? ScreenOrientations,
        int CreativeDurationSeconds,
        decimal? Budget,
        string? CampaignObjective,
        string? Notes);

    private sealed record SimulatorForecastResponse(
        int EligibleScreens,
        int EligibleBuildings,
        long EstimatedPlays,
        long EstimatedAudience,
        decimal EstimatedCost,
        decimal AvailableCapacity,
        List<string> Warnings,
        List<string> Conflicts,
        string SuggestedNextAction);

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
