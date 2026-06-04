using ElevatorAds.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElevatorAds.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Campaigns;

public class CampaignDeliveryConstraintsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public CampaignDeliveryConstraintsEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDeliveryConstraints_WhenNoneExist_ReturnsNotFound()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);

        var response = await client.GetAsync($"/api/campaigns/{campaign.Id}/delivery-constraints");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpsertDeliveryConstraints_ReturnsOk()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var request = new UpsertDeliveryConstraintsRequest(
            new[] { "Lisbon" },
            new[] { "Residential" },
            new[] { "Portrait" },
            new[] { "Monday", "Tuesday" },
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        var response = await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/delivery-constraints", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var constraints = await response.Content.ReadFromJsonAsync<DeliveryConstraintsDto>(JsonOptions);
        Assert.NotNull(constraints);
        Assert.NotEqual(Guid.Empty, constraints!.Id);
        Assert.Equal(campaign.Id, constraints.CampaignId);
        Assert.Equal(request.Cities, constraints.Cities);
        Assert.Equal(new[] { BuildingType.Residential }, constraints.BuildingTypes);
        Assert.Equal(new[] { ScreenOrientation.Portrait }, constraints.ScreenOrientations);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, constraints.DaysOfWeek);
        Assert.Equal(request.StartTime, constraints.StartTime);
        Assert.Equal(request.EndTime, constraints.EndTime);
    }

    [Fact]
    public async Task UpsertDeliveryConstraints_ReplacesExisting()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);

        var firstRequest = new UpsertDeliveryConstraintsRequest(
            new[] { "Lisbon" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null);
        await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/delivery-constraints", firstRequest);

        var secondRequest = new UpsertDeliveryConstraintsRequest(
            new[] { "Porto" },
            new[] { "Corporate" },
            new[] { "Landscape" },
            new[] { "Friday" },
            new TimeOnly(8, 30),
            new TimeOnly(12, 0));

        var response = await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/delivery-constraints", secondRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var constraints = await response.Content.ReadFromJsonAsync<DeliveryConstraintsDto>(JsonOptions);
        Assert.NotNull(constraints);
        Assert.Equal(new[] { "Porto" }, constraints!.Cities);
        Assert.Equal(new[] { BuildingType.Corporate }, constraints.BuildingTypes);
        Assert.Equal(new[] { ScreenOrientation.Landscape }, constraints.ScreenOrientations);
        Assert.Equal(new[] { DayOfWeek.Friday }, constraints.DaysOfWeek);
        Assert.Equal(secondRequest.StartTime, constraints.StartTime);
        Assert.Equal(secondRequest.EndTime, constraints.EndTime);

        var getResponse = await client.GetAsync($"/api/campaigns/{campaign.Id}/delivery-constraints");
        var stored = await getResponse.Content.ReadFromJsonAsync<DeliveryConstraintsDto>(JsonOptions);
        Assert.NotNull(stored);
        Assert.Equal(constraints.Id, stored!.Id);
        Assert.Equal(constraints.Cities, stored.Cities);
    }

    [Fact]
    public async Task UpsertDeliveryConstraints_InvalidCampaignId_Returns422()
    {
        var client = CreateClient();
        var request = new UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null);

        var response = await client.PutAsJsonAsync($"/api/campaigns/{Guid.NewGuid()}/delivery-constraints", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task UpsertDeliveryConstraints_StartTimeAfterEndTime_Returns422()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var request = new UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new TimeOnly(18, 0),
            new TimeOnly(9, 0));

        var response = await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/delivery-constraints", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => new TestWebApplicationFactory().CreateAuthenticatedClient();

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client)
    {
        var request = new CreateAdvertiserRequest(
            "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private async Task<CampaignDto> CreateCampaignAsync(HttpClient client)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            "Summer Push",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            "Draft",
            100m,
            1000m,
            8.5m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);
        response.EnsureSuccessStatusCode();

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        return campaign!;
    }

    private sealed record CreateAdvertiserRequest(
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status);

    private sealed record AdvertiserDto(
        Guid Id,
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record CreateCampaignRequest(
        Guid AdvertiserId,
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        string Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm);

    private sealed record CampaignDto(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        string Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record UpsertDeliveryConstraintsRequest(
        IReadOnlyList<string> Cities,
        IReadOnlyList<string> BuildingTypes,
        IReadOnlyList<string> ScreenOrientations,
        IReadOnlyList<string> DaysOfWeek,
        TimeOnly? StartTime,
        TimeOnly? EndTime);

    private sealed record DeliveryConstraintsDto(
        Guid Id,
        Guid CampaignId,
        IReadOnlyList<string> Cities,
        IReadOnlyList<BuildingType> BuildingTypes,
        IReadOnlyList<ScreenOrientation> ScreenOrientations,
        IReadOnlyList<DayOfWeek> DaysOfWeek,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
