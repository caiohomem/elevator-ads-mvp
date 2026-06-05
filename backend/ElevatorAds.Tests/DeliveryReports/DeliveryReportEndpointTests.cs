using ElevatorAds.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.DeliveryReports;

public class DeliveryReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DeliveryReportEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetOverview_NoData_ReturnsZeroTotalsAndEmptyGroups()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/overview?date=2026-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<OverviewReportDto>();
        Assert.NotNull(report);
        Assert.Equal("2026-01-01", report!.Date);
        Assert.Equal(0, report.TotalPlays);
        Assert.Equal(0L, report.TotalPlayedSeconds);
        Assert.Empty(report.ByCampaign);
        Assert.Empty(report.ByScreen);
        Assert.Empty(report.ByCreative);
    }

    [Fact]
    public async Task GetOverview_WithData_ReturnsTotals()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var date = new DateOnly(2026, 2, 10);
        var screen = await CreateScreenAsync(client, "North Tower Lobby");
        var advertiser = await CreateAdvertiserAsync(client, "Atlas Media");
        var campaign = await CreateCampaignAsync(client, advertiser.Id, "Morning Reach");
        var creative = await CreateCreativeAsync(client, advertiser.Id, "Welcome Loop");
        await SeedEventsAsync(factory,
            BuildEvent(date.ToDateTime(new TimeOnly(8, 0)), screen.Id, campaign.Id, creative.Id, 15),
            BuildEvent(date.ToDateTime(new TimeOnly(9, 0)), screen.Id, campaign.Id, creative.Id, 20));

        var response = await client.GetAsync($"/api/reports/overview?date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<OverviewReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.TotalPlays);
        Assert.Equal(35L, report.TotalPlayedSeconds);
        var campaignSummary = Assert.Single(report.ByCampaign);
        Assert.Equal(campaign.Id, campaignSummary.Id);
        Assert.Equal(campaign.Name, campaignSummary.Name);
        Assert.Equal(2, campaignSummary.Plays);
        Assert.Equal(35L, campaignSummary.PlayedSeconds);
        var screenSummary = Assert.Single(report.ByScreen);
        Assert.Equal(screen.Id, screenSummary.Id);
        Assert.Equal(screen.Name, screenSummary.Name);
        var creativeSummary = Assert.Single(report.ByCreative);
        Assert.Equal(creative.Id, creativeSummary.Id);
        Assert.Equal(creative.Name, creativeSummary.Name);
    }

    [Fact]
    public async Task GetOverview_GroupsByCampaignScreenAndCreative()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var date = new DateOnly(2026, 3, 15);
        var firstScreen = Guid.NewGuid();
        var secondScreen = Guid.NewGuid();
        var firstCampaign = Guid.NewGuid();
        var secondCampaign = Guid.NewGuid();
        var firstCreative = Guid.NewGuid();
        var secondCreative = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(date.ToDateTime(new TimeOnly(8, 0)), firstScreen, firstCampaign, firstCreative, 10),
            BuildEvent(date.ToDateTime(new TimeOnly(9, 0)), firstScreen, firstCampaign, firstCreative, 10),
            BuildEvent(date.ToDateTime(new TimeOnly(10, 0)), secondScreen, secondCampaign, secondCreative, 5));

        var response = await client.GetAsync($"/api/reports/overview?date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<OverviewReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.ByCampaign.Count);
        Assert.Equal(2, report.ByScreen.Count);
        Assert.Equal(2, report.ByCreative.Count);
        var firstCampaignSummary = report.ByCampaign.Single(item => item.Id == firstCampaign);
        Assert.Equal(2, firstCampaignSummary.Plays);
        Assert.Equal(20L, firstCampaignSummary.PlayedSeconds);
        var secondCampaignSummary = report.ByCampaign.Single(item => item.Id == secondCampaign);
        Assert.Equal(1, secondCampaignSummary.Plays);
        Assert.Equal(5L, secondCampaignSummary.PlayedSeconds);
    }

    [Fact]
    public async Task GetOverview_ExcludesEventsOutsideDate()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var date = new DateOnly(2026, 4, 1);
        var screenId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(date.AddDays(-1).ToDateTime(new TimeOnly(23, 59)), screenId, campaignId, creativeId, 10),
            BuildEvent(date.ToDateTime(new TimeOnly(0, 0)), screenId, campaignId, creativeId, 15),
            BuildEvent(date.AddDays(1).ToDateTime(new TimeOnly(0, 0)), screenId, campaignId, creativeId, 20));

        var response = await client.GetAsync($"/api/reports/overview?date={date:yyyy-MM-dd}");

        var report = await response.Content.ReadFromJsonAsync<OverviewReportDto>();
        Assert.NotNull(report);
        Assert.Equal(1, report!.TotalPlays);
        Assert.Equal(15L, report.TotalPlayedSeconds);
    }

    [Fact]
    public async Task GetOverview_MissingDate_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/overview");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_InvalidDate_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/overview?date=not-a-date");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCampaigns_GroupsByCampaign()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var screen = await CreateScreenAsync(client, "West Lift Screen");
        var advertiser = await CreateAdvertiserAsync(client, "Northwind");
        var firstCampaign = await CreateCampaignAsync(client, advertiser.Id, "Residency Launch");
        var secondCampaign = await CreateCampaignAsync(client, advertiser.Id, "Gym Retargeting");
        var creative = await CreateCreativeAsync(client, advertiser.Id, "Loop A");
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 5, 1, 10, 0, 0), screen.Id, firstCampaign.Id, creative.Id, 10),
            BuildEvent(new DateTime(2026, 5, 2, 10, 0, 0), screen.Id, firstCampaign.Id, creative.Id, 20),
            BuildEvent(new DateTime(2026, 5, 3, 10, 0, 0), screen.Id, secondCampaign.Id, creative.Id, 30));

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-05-01&to=2026-05-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal("2026-05-01", report!.From);
        Assert.Equal("2026-05-03", report.To);
        Assert.Equal(3, report.TotalPlays);
        Assert.Equal(60L, report.TotalPlayedSeconds);
        Assert.Equal(2, report.Campaigns.Count);
        var firstSummary = report.Campaigns.Single(item => item.Id == firstCampaign.Id);
        Assert.Equal(firstCampaign.Name, firstSummary.Name);
        Assert.Equal(2, firstSummary.Plays);
        Assert.Equal(30L, firstSummary.PlayedSeconds);
        var secondSummary = report.Campaigns.Single(item => item.Id == secondCampaign.Id);
        Assert.Equal(secondCampaign.Name, secondSummary.Name);
        Assert.Equal(1, secondSummary.Plays);
        Assert.Equal(30L, secondSummary.PlayedSeconds);
    }

    [Fact]
    public async Task GetCampaigns_FiltersByDateRange()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var campaignId = Guid.NewGuid();
        var screenId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 5, 9, 23, 59, 0), screenId, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 5, 10, 0, 0, 0), screenId, campaignId, creativeId, 15),
            BuildEvent(new DateTime(2026, 5, 15, 12, 0, 0), screenId, campaignId, creativeId, 20),
            BuildEvent(new DateTime(2026, 5, 16, 0, 0, 0), screenId, campaignId, creativeId, 25));

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-05-10&to=2026-05-15");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.TotalPlays);
        Assert.Equal(35L, report.TotalPlayedSeconds);
        var summary = Assert.Single(report.Campaigns);
        Assert.Equal(campaignId, summary.Id);
        Assert.Equal(2, summary.Plays);
    }

    [Fact]
    public async Task GetCampaigns_EmptyRange_ReturnsEmptyResults()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-06-01&to=2026-06-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal(0, report!.TotalPlays);
        Assert.Equal(0L, report.TotalPlayedSeconds);
        Assert.Empty(report.Campaigns);
    }

    [Fact]
    public async Task GetCampaigns_MissingDateRange_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-06-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCampaigns_ToBeforeFrom_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-06-05&to=2026-06-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetScreens_GroupsByScreen()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var firstScreen = await CreateScreenAsync(client, "Lobby Portrait");
        var secondScreen = await CreateScreenAsync(client, "Garage Entrance");
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 7, 1, 10, 0, 0), firstScreen.Id, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 7, 2, 10, 0, 0), firstScreen.Id, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 7, 3, 10, 0, 0), secondScreen.Id, campaignId, creativeId, 20));

        var response = await client.GetAsync("/api/reports/screens?from=2026-07-01&to=2026-07-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ScreenReportDto>();
        Assert.NotNull(report);
        Assert.Equal("2026-07-01", report!.From);
        Assert.Equal("2026-07-03", report.To);
        Assert.Equal(3, report.TotalPlays);
        Assert.Equal(40L, report.TotalPlayedSeconds);
        Assert.Equal(2, report.Screens.Count);
        var firstSummary = report.Screens.Single(item => item.Id == firstScreen.Id);
        Assert.Equal(firstScreen.Name, firstSummary.Name);
        Assert.Equal(2, firstSummary.Plays);
        Assert.Equal(20L, firstSummary.PlayedSeconds);
        var secondSummary = report.Screens.Single(item => item.Id == secondScreen.Id);
        Assert.Equal(secondScreen.Name, secondSummary.Name);
    }

    [Fact]
    public async Task GetScreens_FiltersByDateRange()
    {
        await _factory.ResetDatabaseAsync();
        var (client, factory) = CreateClientWithFactory();
        var screenId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 7, 9, 23, 0, 0), screenId, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 7, 10, 10, 0, 0), screenId, campaignId, creativeId, 15),
            BuildEvent(new DateTime(2026, 7, 12, 0, 0, 0), screenId, campaignId, creativeId, 20),
            BuildEvent(new DateTime(2026, 7, 13, 0, 0, 0), screenId, campaignId, creativeId, 25));

        var response = await client.GetAsync("/api/reports/screens?from=2026-07-10&to=2026-07-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ScreenReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.TotalPlays);
        Assert.Equal(35L, report.TotalPlayedSeconds);
        var summary = Assert.Single(report.Screens);
        Assert.Equal(screenId, summary.Id);
        Assert.Equal(2, summary.Plays);
    }

    [Fact]
    public async Task GetScreens_EmptyRange_ReturnsEmptyResults()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/screens?from=2026-08-01&to=2026-08-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ScreenReportDto>();
        Assert.NotNull(report);
        Assert.Equal(0, report!.TotalPlays);
        Assert.Equal(0L, report.TotalPlayedSeconds);
        Assert.Empty(report.Screens);
    }

    [Fact]
    public async Task GetScreens_MissingDateRange_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/screens?from=2026-08-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private (HttpClient Client, TestWebApplicationFactory Factory) CreateClientWithFactory()
    {
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        return (client, _factory);
    }

    private static async Task SeedEventsAsync(TestWebApplicationFactory factory, params ProofOfPlayEvent[] events)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProofOfPlayEventRepository>();
        foreach (var item in events)
        {
            await repository.AddAsync(item);
        }
    }

    private static ProofOfPlayEvent BuildEvent(
        DateTime playedAt,
        Guid screenId,
        Guid campaignId,
        Guid creativeId,
        int durationSeconds) => new()
        {
            Id = Guid.NewGuid(),
            ScreenId = screenId,
            PlaylistId = Guid.NewGuid(),
            PlaylistItemId = Guid.NewGuid(),
            CampaignId = campaignId,
            CreativeId = creativeId,
            PlayedAt = playedAt,
            DurationSeconds = durationSeconds,
            CreatedAt = DateTime.UtcNow
        };

    private async Task<ScreenDto> CreateScreenAsync(HttpClient client, string name)
    {
        var building = await CreateBuildingAsync(client, $"{name} Building");
        var request = new CreateScreenRequest(
            building.Id,
            name,
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

    private async Task<BuildingDto> CreateBuildingAsync(HttpClient client, string name)
    {
        var request = new CreateBuildingRequest(
            name,
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

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client, string name)
    {
        var request = new CreateAdvertiserRequest(
            name,
            $"{name} LLC",
            $"{Random.Shared.NextInt64(100000000, 999999999)}",
            "Jane Doe",
            $"{Guid.NewGuid():N}@example.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);
        response.EnsureSuccessStatusCode();
        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private async Task<CampaignDto> CreateCampaignAsync(HttpClient client, Guid advertiserId, string name)
    {
        var request = new CreateCampaignRequest(
            advertiserId,
            name,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            "Active",
            100m,
            1000m,
            8m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);
        response.EnsureSuccessStatusCode();
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        return campaign!;
    }

    private async Task<CreativeDto> CreateCreativeAsync(HttpClient client, Guid advertiserId, string name)
    {
        var request = new CreateCreativeRequest(
            advertiserId,
            name,
            $"https://cdn.example.com/{Guid.NewGuid():N}.jpg",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);
        response.EnsureSuccessStatusCode();
        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        return creative!;
    }

    private sealed record GroupSummaryDto(Guid Id, string Name, int Plays, long PlayedSeconds);

    private sealed record OverviewReportDto(
        string Date,
        int TotalPlays,
        long TotalPlayedSeconds,
        IReadOnlyList<GroupSummaryDto> ByCampaign,
        IReadOnlyList<GroupSummaryDto> ByScreen,
        IReadOnlyList<GroupSummaryDto> ByCreative);

    private sealed record CampaignReportDto(
        string From,
        string To,
        int TotalPlays,
        long TotalPlayedSeconds,
        IReadOnlyList<GroupSummaryDto> Campaigns);

    private sealed record ScreenReportDto(
        string From,
        string To,
        int TotalPlays,
        long TotalPlayedSeconds,
        IReadOnlyList<GroupSummaryDto> Screens);

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
}
