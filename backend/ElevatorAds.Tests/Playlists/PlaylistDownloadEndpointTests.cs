using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.Playlists;

public class PlaylistDownloadEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlaylistDownloadEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetCurrentPlaylist_ReturnsPublishedPlaylist()
    {
        var (client, _) = CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlist/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playlist = await response.Content.ReadFromJsonAsync<PlaylistDownloadDto>();
        Assert.NotNull(playlist);
        Assert.Equal(generated.Id, playlist!.PlaylistId);
        Assert.Equal(screen.Id, playlist.ScreenId);
        Assert.Equal("Published", playlist.Status);
        Assert.NotEmpty(playlist.Items);
        Assert.Equal(creative.MediaUrl, playlist.Items[0].MediaUrl);
        Assert.Equal(creative.MediaType, playlist.Items[0].MediaType);
    }

    [Fact]
    public async Task GetPlaylistByDate_ReturnsPublishedPlaylist()
    {
        var (client, _) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", date);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, date);
        await PublishAsync(client, generated.Id);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlist?date=2026-06-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playlist = await response.Content.ReadFromJsonAsync<PlaylistDownloadDto>();
        Assert.NotNull(playlist);
        Assert.Equal(generated.Id, playlist!.PlaylistId);
        Assert.Equal("2026-06-01", playlist.Date);
    }

    [Fact]
    public async Task GetPlaylistByDate_ReturnsLatestPublishedVersion()
    {
        var (client, _) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", date);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var first = await GenerateForScreenAsync(client, screen.Id, date);
        await PublishAsync(client, first.Id);
        var second = await GenerateForScreenAsync(client, screen.Id, date);
        await PublishAsync(client, second.Id);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlist?date=2026-06-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playlist = await response.Content.ReadFromJsonAsync<PlaylistDownloadDto>();
        Assert.NotNull(playlist);
        Assert.Equal(2, playlist!.Version);
        Assert.Equal(second.Id, playlist.PlaylistId);
    }

    [Fact]
    public async Task GetPlaylistByDate_DraftPlaylist_Returns404()
    {
        var (client, _) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", date);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        await GenerateForScreenAsync(client, screen.Id, date);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlist?date=2026-06-01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkPlaylistDownloaded_ReturnsDownloadedStatus()
    {
        var (client, _) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", date);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, date);
        await PublishAsync(client, generated.Id);

        var response = await client.PostAsync($"/api/screens/{screen.Id}/playlist/{generated.Id}/downloaded", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playlist = await response.Content.ReadFromJsonAsync<PlaylistDownloadDto>();
        Assert.NotNull(playlist);
        Assert.Equal("Downloaded", playlist!.Status);
        Assert.Equal(generated.Id, playlist.PlaylistId);
    }

    [Fact]
    public async Task MarkPlaylistDownloaded_WrongScreen_Returns404()
    {
        var (client, _) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var otherScreen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", date);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, date);
        await PublishAsync(client, generated.Id);

        var response = await client.PostAsync($"/api/screens/{otherScreen.Id}/playlist/{generated.Id}/downloaded", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlaylistItems_AreInDeterministicOrder()
    {
        var (client, factory) = CreateClient();
        var date = new DateOnly(2026, 6, 1);
        var screen = await CreateScreenAsync(client);
        var firstCampaign = await CreateCampaignAsync(client, "Active", date);
        var secondCampaign = await CreateCampaignAsync(client, "Active", date);
        var firstCreative = await CreateAndApproveCreativeAsync(client);
        var secondCreative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, firstCampaign.Id, firstCreative.Id);
        await AssignCreativeAsync(client, secondCampaign.Id, secondCreative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, date);

        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IDailyPlaylistRepository>();
            var playlist = await repository.GetByIdAsync(generated.Id);
            Assert.NotNull(playlist);
            playlist!.Items = playlist.Items.OrderByDescending(item => item.Order).ToList();
            await repository.UpdateAsync(playlist);
        }

        await PublishAsync(client, generated.Id);

        var response = await client.GetAsync($"/api/screens/{screen.Id}/playlist?date=2026-06-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playlistDownload = await response.Content.ReadFromJsonAsync<PlaylistDownloadDto>();
        Assert.NotNull(playlistDownload);
        Assert.Equal(
            playlistDownload!.Items.Select(item => item.Order).OrderBy(order => order),
            playlistDownload.Items.Select(item => item.Order));
    }

    private (HttpClient Client, WebApplicationFactory<Program> Factory) CreateClient()
    {
        var factory = _factory.WithWebHostBuilder(_ => { });
        return (factory.CreateClient(), factory);
    }

    private async Task<DailyPlaylistDto> GenerateForScreenAsync(HttpClient client, Guid screenId, DateOnly date)
    {
        var response = await client.PostAsync($"/api/playlists/generate?date={date:yyyy-MM-dd}", null);
        response.EnsureSuccessStatusCode();

        var playlists = await response.Content.ReadFromJsonAsync<List<DailyPlaylistDto>>();
        Assert.NotNull(playlists);
        return Assert.Single(playlists!, item => item.ScreenId == screenId);
    }

    private static async Task<DailyPlaylistDto> PublishAsync(HttpClient client, Guid playlistId)
    {
        var response = await client.PostAsync($"/api/playlists/{playlistId}/publish", null);
        response.EnsureSuccessStatusCode();

        var playlist = await response.Content.ReadFromJsonAsync<DailyPlaylistDto>();
        Assert.NotNull(playlist);
        return playlist!;
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

    private async Task<CampaignDto> CreateCampaignAsync(HttpClient client, string campaignStatus, DateOnly activeDate)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            $"Campaign-{Guid.NewGuid():N}",
            activeDate.AddDays(-10).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            activeDate.AddDays(10).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            campaignStatus,
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

    private sealed record PlaylistDownloadItemDto(
        int Order,
        Guid CampaignId,
        Guid CreativeId,
        string MediaUrl,
        string MediaType,
        int DurationSeconds);

    private sealed record PlaylistDownloadDto(
        Guid PlaylistId,
        Guid ScreenId,
        string Date,
        int Version,
        string Status,
        DateTime GeneratedAt,
        DateTime? PublishedAt,
        List<PlaylistDownloadItemDto> Items);
}
