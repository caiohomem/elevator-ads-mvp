using ElevatorAds.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.PlaybackReports;

public class PlaybackReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlaybackReportEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SubmitPlaybackReport_ReturnsCreatedWithResolvedIds()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);
        var playedAt = DateTime.UtcNow;

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            playedAt,
            15);

        var response = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PlaybackReportDto>();
        Assert.NotNull(report);
        Assert.NotEqual(Guid.Empty, report!.Id);
        Assert.Equal(screen.Id, report.ScreenId);
        Assert.Equal(generated.Id, report.PlaylistId);
        Assert.Equal(item.Id, report.PlaylistItemId);
        Assert.Equal(campaign.Id, report.CampaignId);
        Assert.Equal(creative.Id, report.CreativeId);
        Assert.Equal(15, report.DurationSeconds);
    }

    [Fact]
    public async Task SubmitPlaybackReport_UnknownScreen_Returns404()
    {
        var client = CreateClient();
        var request = new CreatePlaybackReportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            10);

        var response = await client.PostAsJsonAsync($"/api/screens/{Guid.NewGuid()}/playback-reports", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitPlaybackReport_PlaylistFromOtherScreen_Returns404()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var otherScreen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            DateTime.UtcNow,
            10);

        var response = await client.PostAsJsonAsync($"/api/screens/{otherScreen.Id}/playback-reports", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitPlaybackReport_InvalidPlaylistItem_Returns404()
    {
        var client = CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            Guid.NewGuid(),
            DateTime.UtcNow,
            10);

        var response = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitPlaybackReport_MissingPlayedAt_Returns422()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            null,
            10);

        var response = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SubmitPlaybackReport_NonPositiveDuration_Returns422()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            DateTime.UtcNow,
            0);

        var response = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ListPlaybackReportsByScreen_ReturnsSubmittedReport()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            DateTime.UtcNow,
            15);

        var postResponse = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);
        postResponse.EnsureSuccessStatusCode();
        var submitted = await postResponse.Content.ReadFromJsonAsync<PlaybackReportDto>();
        Assert.NotNull(submitted);

        var listResponse = await client.GetAsync($"/api/screens/{screen.Id}/playback-reports");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var reports = await listResponse.Content.ReadFromJsonAsync<List<PlaybackReportDto>>();
        Assert.NotNull(reports);
        Assert.Contains(reports!, report => report.Id == submitted!.Id);
    }

    [Fact]
    public async Task ListPlaybackReportsByCampaign_ReturnsSubmittedReport()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            DateTime.UtcNow,
            15);

        var postResponse = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);
        postResponse.EnsureSuccessStatusCode();
        var submitted = await postResponse.Content.ReadFromJsonAsync<PlaybackReportDto>();
        Assert.NotNull(submitted);

        var listResponse = await client.GetAsync($"/api/campaigns/{campaign.Id}/playback-reports");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var reports = await listResponse.Content.ReadFromJsonAsync<List<PlaybackReportDto>>();
        Assert.NotNull(reports);
        Assert.Contains(reports!, report => report.Id == submitted!.Id);
    }

    [Fact]
    public async Task ListAllPlaybackReports_ReturnsSubmittedReport()
    {
        var (client, factory) = CreateClientWithFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var screen = await CreateScreenAsync(client);
        var campaign = await CreateCampaignAsync(client, "Active", today);
        var creative = await CreateAndApproveCreativeAsync(client);
        await AssignCreativeAsync(client, campaign.Id, creative.Id);
        var generated = await GenerateForScreenAsync(client, screen.Id, today);
        await PublishAsync(client, generated.Id);
        var item = await GetFirstPlaylistItemAsync(factory, generated.Id);

        var request = new CreatePlaybackReportRequest(
            generated.Id,
            item.Id,
            DateTime.UtcNow,
            15);

        var postResponse = await client.PostAsJsonAsync($"/api/screens/{screen.Id}/playback-reports", request);
        postResponse.EnsureSuccessStatusCode();
        var submitted = await postResponse.Content.ReadFromJsonAsync<PlaybackReportDto>();
        Assert.NotNull(submitted);

        var listResponse = await client.GetAsync("/api/playback-reports");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var reports = await listResponse.Content.ReadFromJsonAsync<List<PlaybackReportDto>>();
        Assert.NotNull(reports);
        Assert.Contains(reports!, report => report.Id == submitted!.Id);
    }

    private (HttpClient Client, WebApplicationFactory<Program> Factory) CreateClientWithFactory()
    {
        var factory = _factory.WithWebHostBuilder(_ => { });
        return (factory.CreateClient(), factory);
    }

    private HttpClient CreateClient() => CreateClientWithFactory().Client;

    private static async Task<PlaylistItemRef> GetFirstPlaylistItemAsync(WebApplicationFactory<Program> factory, Guid playlistId)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDailyPlaylistRepository>();
        var playlist = await repository.GetByIdAsync(playlistId);
        Assert.NotNull(playlist);
        var item = Assert.Single(playlist!.Items);
        return new PlaylistItemRef(item.Id, item.CampaignId, item.CreativeId);
    }

    private sealed record PlaylistItemRef(Guid Id, Guid CampaignId, Guid CreativeId);

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

    private sealed record CreatePlaybackReportRequest(
        Guid PlaylistId,
        Guid PlaylistItemId,
        DateTime? PlayedAt,
        int DurationSeconds);

    private sealed record PlaybackReportDto(
        Guid Id,
        Guid ScreenId,
        Guid PlaylistId,
        Guid PlaylistItemId,
        Guid CampaignId,
        Guid CreativeId,
        DateTime PlayedAt,
        int DurationSeconds,
        DateTime CreatedAt);
}
