using ElevatorAds.Tests.Infrastructure;
using ElevatorAds.Domain.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Playlists;

public class PlaylistEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlaylistEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GeneratePlaylists_ReturnsGeneratedPlaylist()
    {
        var client = CreateClient();
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, CampaignStatus: "Active");
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);

        var response = await client.PostAsync("/api/playlists/generate?date=2026-06-01", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var playlists = await response.Content.ReadFromJsonAsync<List<DailyPlaylistDto>>();
        Assert.NotNull(playlists);
        var playlist = Assert.Single(playlists!, item => item.ScreenId == screen.Id);
        Assert.Equal("Draft", playlist.Status);
        Assert.Single(playlist.Items);
    }

    [Fact]
    public async Task GetPlaylists_ReturnsPagedResult_AndSupportsStatusFiltering()
    {
        var client = CreateClient();
        var screenOne = await CreateScreenAsync(client);
        var screenTwo = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, CampaignStatus: "Active");
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);

        await GenerateForScreenAsync(client, screenOne.Id);
        await GenerateForScreenAsync(client, screenTwo.Id);

        var pageResponse = await client.GetAsync("/api/playlists?page=1&pageSize=1");
        var sortedResponse = await client.GetAsync("/api/playlists?sortBy=date&sortDirection=asc");
        var statusResponse = await client.GetAsync("/api/playlists?status=Draft");
        var invalidPageResponse = await client.GetAsync("/api/playlists?page=0");

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sortedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageResponse.StatusCode);

        var page = await pageResponse.Content.ReadFromJsonAsync<PagedResult<DailyPlaylistDto>>();
        var sorted = await sortedResponse.Content.ReadFromJsonAsync<PagedResult<DailyPlaylistDto>>();
        var statusSet = await statusResponse.Content.ReadFromJsonAsync<PagedResult<DailyPlaylistDto>>();

        Assert.NotNull(page);
        Assert.NotNull(sorted);
        Assert.NotNull(statusSet);
        Assert.Equal(1, page!.Items.Count);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.True(DateOnly.Parse(sorted!.Items[0].Date) <= DateOnly.Parse(sorted.Items[1].Date));
        Assert.All(statusSet!.Items, item => Assert.Equal("Draft", item.Status));
        Assert.Contains(page.Items, item => item.ScreenId == screenOne.Id || item.ScreenId == screenTwo.Id);
    }

    [Fact]
    public async Task GetPlaylistById_AndPublishPlaylist_ReturnExpectedPayloads()
    {
        var client = CreateClient();
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, CampaignStatus: "Active");
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id);

        var getResponse = await client.GetAsync($"/api/playlists/{generated.Id}");
        var publishResponse = await client.PostAsync($"/api/playlists/{generated.Id}/publish", null);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<DailyPlaylistDto>();
        var published = await publishResponse.Content.ReadFromJsonAsync<DailyPlaylistDto>();
        Assert.NotNull(fetched);
        Assert.NotNull(published);
        Assert.Equal(generated.Id, fetched!.Id);
        Assert.Equal("Published", published!.Status);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task GetScreenPlaylists_ByDate_ReturnsPlaylist()
    {
        var client = CreateClient();
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, CampaignStatus: "Active");
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        await GenerateForScreenAsync(client, screen.Id);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlists?date=2026-06-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var playlists = await response.Content.ReadFromJsonAsync<List<DailyPlaylistDto>>();
        Assert.NotNull(playlists);
        Assert.Single(playlists!);
        Assert.Equal(screen.Id, playlists[0].ScreenId);
        Assert.Equal("2026-06-01", playlists[0].Date);
    }

    [Fact]
    public async Task GeneratePlaylists_WithoutDate_Returns422()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/playlists/generate", null);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task GetScreenPlaylists_WithMissingScreen_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/screens/{Guid.NewGuid()}/playlists?date=2026-06-01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.WithWebHostBuilder(_ => { }).CreateClient();

    private async Task<DailyPlaylistDto> GenerateForScreenAsync(HttpClient client, Guid screenId)
    {
        var response = await client.PostAsync("/api/playlists/generate?date=2026-06-01", null);
        response.EnsureSuccessStatusCode();

        var playlists = await response.Content.ReadFromJsonAsync<List<DailyPlaylistDto>>();
        Assert.NotNull(playlists);
        return Assert.Single(playlists!, item => item.ScreenId == screenId);
    }

    private async Task<BuildingDto> CreateBuildingAsync(HttpClient client)
    {
        var request = new CreateBuildingRequest(
            $"Tower-{Guid.NewGuid():N}",
            "123 Main St",
            "Lisbon",
            "Portugal",
            "1000-001",
            "Residential",
            500);

        var response = await client.PostAsJsonAsync("/api/buildings", request);
        response.EnsureSuccessStatusCode();

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        return building!;
    }

    private async Task<ScreenDto> CreateScreenAsync(HttpClient client)
    {
        var building = await CreateBuildingAsync(client);
        var request = new CreateScreenRequest(
            building.Id,
            $"Lobby Screen-{Guid.NewGuid():N}",
            $"SCR-{Guid.NewGuid():N}",
            1080,
            1920,
            "Portrait",
            "Active");

        var response = await client.PostAsJsonAsync("/api/screens", request);
        response.EnsureSuccessStatusCode();

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        return screen!;
    }

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client)
    {
        var request = new CreateAdvertiserRequest(
            $"Acme-{Guid.NewGuid():N}",
            "Acme Holdings Ltd",
            $"{Random.Shared.NextInt64(100000000, 999999999)}",
            "Jane Doe",
            $"{Guid.NewGuid():N}@acme.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private async Task<CampaignDto> CreateCampaignAsync(HttpClient client, string CampaignStatus)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            $"Campaign-{Guid.NewGuid():N}",
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            CampaignStatus,
            100m,
            1000m,
            8.5m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);
        response.EnsureSuccessStatusCode();

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        return campaign!;
    }

    private async Task<CreativeDto> CreateAndApproveCreativeAsync(HttpClient client)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            $"Creative-{Guid.NewGuid():N}",
            $"https://cdn.example.com/{Guid.NewGuid():N}.jpg",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);
        response.EnsureSuccessStatusCode();

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);

        var submitResponse = await client.PostAsync($"/api/creatives/{creative!.Id}/submit-for-review", null);
        submitResponse.EnsureSuccessStatusCode();

        var approveResponse = await client.PostAsync($"/api/creatives/{creative.Id}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        var approvedCreative = await approveResponse.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(approvedCreative);
        return approvedCreative!;
    }

    private static async Task AssignCreativeAsync(HttpClient client, Guid campaignId, Guid creativeId)
    {
        var response = await client.PostAsync($"/api/campaigns/{campaignId}/creatives/{creativeId}", null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record CreateBuildingRequest(
        string Name,
        string Address,
        string City,
        string Country,
        string PostalCode,
        string BuildingType,
        int EstimatedDailyAudience);

    private sealed record BuildingDto(
        Guid Id,
        string Name,
        string Address,
        string City,
        string Country,
        string PostalCode,
        string BuildingType,
        int EstimatedDailyAudience,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record CreateScreenRequest(
        Guid BuildingId,
        string Name,
        string ExternalCode,
        int ResolutionWidth,
        int ResolutionHeight,
        string Orientation,
        string Status);

    private sealed record ScreenDto(
        Guid Id,
        Guid BuildingId,
        string Name,
        string ExternalCode,
        int ResolutionWidth,
        int ResolutionHeight,
        string Orientation,
        string Status,
        DateTime? LastSeenAt,
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

    private sealed record CreateCreativeRequest(
        Guid AdvertiserId,
        string Name,
        string MediaUrl,
        string MediaType,
        int DurationSeconds);

    private sealed record CreativeDto(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        string MediaUrl,
        string MediaType,
        int DurationSeconds,
        string ApprovalStatus,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record DailyPlaylistItemDto(
        Guid Id,
        Guid DailyPlaylistId,
        Guid CampaignId,
        Guid CreativeId,
        int Order,
        int DurationSeconds,
        DateTime CreatedAt);

    private sealed record DailyPlaylistDto(
        Guid Id,
        Guid ScreenId,
        string Date,
        int Version,
        string Status,
        DateTime GeneratedAt,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<DailyPlaylistItemDto> Items);
}
