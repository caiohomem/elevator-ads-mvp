using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Campaigns;

public sealed class CampaignForecastEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CampaignForecastEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostForecast_ValidBookingRequest_ReturnsForecast()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 600);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Equal(bookingRequest.Id, forecast!.BookingRequestId);
        Assert.Equal(1, forecast.EligibleScreens);
        Assert.Equal(1, forecast.EligibleBuildings);
        Assert.Equal(480, forecast.EstimatedPlays);
        Assert.Equal(9000, forecast.EstimatedAudience);
        Assert.Equal(4.80m, forecast.EstimatedCost);
    }

    [Fact]
    public async Task GetForecast_ReturnsLatestForecast()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 600);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id);

        var generateResponse = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);
        generateResponse.EnsureSuccessStatusCode();
        var generated = await generateResponse.Content.ReadFromJsonAsync<CampaignForecastDto>();

        var response = await client.GetAsync($"/api/booking-requests/{bookingRequest.Id}/forecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.NotNull(generated);
        Assert.Equal(generated!.Id, forecast!.Id);
        Assert.Equal(generated.UpdatedAt, forecast.UpdatedAt);
    }

    [Fact]
    public async Task PostForecast_FiltersCity()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var lisbon = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 700);
        var porto = await CreateBuildingAsync(client, city: "Porto", buildingType: "Corporate", audience: 500);
        await CreateScreenAsync(client, lisbon.Id, orientation: "Portrait", status: "Active");
        await CreateScreenAsync(client, porto.Id, orientation: "Portrait", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id, cities: ["Lisbon"]);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Equal(1, forecast!.EligibleScreens);
        Assert.Equal(1, forecast.EligibleBuildings);
    }

    [Fact]
    public async Task PostForecast_FiltersBuildingType()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var corporate = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 700);
        var commercial = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Commercial", audience: 500);
        await CreateScreenAsync(client, corporate.Id, orientation: "Portrait", status: "Active");
        await CreateScreenAsync(client, commercial.Id, orientation: "Portrait", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id, buildingTypes: ["Commercial"]);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Equal(1, forecast!.EligibleScreens);
        Assert.Equal(1, forecast.EligibleBuildings);
        Assert.Equal(500 * 15, forecast.EstimatedAudience);
    }

    [Fact]
    public async Task PostForecast_FiltersScreenOrientation()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 700);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait", status: "Active");
        await CreateScreenAsync(client, building.Id, orientation: "Landscape", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id, screenOrientations: ["Landscape"]);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Equal(1, forecast!.EligibleScreens);
    }

    [Fact]
    public async Task PostForecast_NoMatchingInventory_ReturnsZeroEligible()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Porto", buildingType: "Commercial", audience: 400);
        await CreateScreenAsync(client, building.Id, orientation: "Landscape", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(
            client,
            advertiser.Id,
            cities: ["Lisbon"],
            buildingTypes: ["Corporate"],
            screenOrientations: ["Portrait"]);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Equal(0, forecast!.EligibleScreens);
        Assert.Equal(0, forecast.EligibleBuildings);
        Assert.Equal(0, forecast.EstimatedPlays);
        Assert.Equal(0, forecast.EstimatedAudience);
    }

    [Fact]
    public async Task PostForecast_IncludesWarningsWhenDataIsIncomplete()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate", audience: 0);
        await CreateScreenAsync(client, building.Id, orientation: "Portrait", status: "Active");
        var bookingRequest = await CreateBookingRequestAsync(client, advertiser.Id);

        var response = await client.PostAsync($"/api/booking-requests/{bookingRequest.Id}/forecast", null);

        response.EnsureSuccessStatusCode();

        var forecast = await response.Content.ReadFromJsonAsync<CampaignForecastDto>();
        Assert.NotNull(forecast);
        Assert.Contains(forecast!.Warnings, item => item.Contains("missing audience data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forecast.Warnings, item => item.Contains("placeholder CPM", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostForecast_InvalidBookingRequest_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.PostAsync($"/api/booking-requests/{Guid.NewGuid()}/forecast", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static async Task CreateScreenAsync(
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
    }

    private static async Task<BookingRequestDto> CreateBookingRequestAsync(
        HttpClient client,
        Guid advertiserId,
        List<string>? cities = null,
        List<string>? buildingTypes = null,
        List<string>? screenOrientations = null,
        int creativeDurationSeconds = 15,
        decimal budget = 500m)
    {
        var response = await client.PostAsJsonAsync("/api/booking-requests", new CreateBookingRequestRequest(
            advertiserId,
            "Forecast Request",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            cities ?? ["Lisbon"],
            buildingTypes ?? ["Corporate"],
            screenOrientations ?? ["Portrait"],
            creativeDurationSeconds,
            budget,
            "Brand awareness",
            "Forecast test"));
        response.EnsureSuccessStatusCode();

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        return bookingRequest!;
    }

    private sealed record CampaignForecastDto(
        Guid Id,
        Guid BookingRequestId,
        int EligibleScreens,
        int EligibleBuildings,
        long EstimatedPlays,
        long EstimatedAudience,
        decimal EstimatedCost,
        decimal AvailableCapacity,
        List<string> Warnings,
        List<string> Conflicts,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record CreateAdvertiserRequest(
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status);

    private sealed record AdvertiserDto(Guid Id, string Name);

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
}
