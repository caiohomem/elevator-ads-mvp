using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Infrastructure.Persistence;
using ElevatorAds.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.Reports;

public class EstimatedProofOfPlayEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EstimatedProofOfPlayEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetEstimatedProofOfPlay_MissingCampaignId_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        var response = await client.GetAsync("/api/reports/estimated-proof-of-play?from=2026-06-01&to=2026-06-02");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_CampaignNotFound_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={Guid.NewGuid()}&from=2026-06-01&to=2026-06-02");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_UsesReportedEventsWhenAvailable()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();
        var playedAt = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

        await SeedProofOfPlayEventAsync(new ProofOfPlayEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = scenario.OrganizationId,
            ScreenId = scenario.ScreenId,
            PlaylistId = scenario.PlaylistId,
            PlaylistItemId = scenario.PlaylistItemId,
            CampaignId = scenario.CampaignId,
            CreativeId = scenario.CreativeId,
            PlayedAt = playedAt,
            DurationSeconds = 15,
            CreatedAt = DateTime.UtcNow
        });

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={scenario.CampaignId}&from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<EstimatedProofOfPlayReportDto>();
        Assert.NotNull(report);
        Assert.Equal(1, report!.TotalScheduledPlays);
        Assert.Equal(1, report.TotalReportedPlays);
        var item = Assert.Single(report.Items);
        Assert.Equal(1, item.ReportedPlays);
        Assert.Equal(1, item.ScheduledPlays);
        Assert.DoesNotContain(report.Warnings, warning => warning.Contains("scheduled playlist data", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_FallsBackToScheduledPlaylistWhenNoEventsExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={scenario.CampaignId}&from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<EstimatedProofOfPlayReportDto>();
        Assert.NotNull(report);
        var item = Assert.Single(report!.Items);
        Assert.Equal(1, item.ScheduledPlays);
        Assert.Equal(0, item.ReportedPlays);
        Assert.Contains(report.Warnings, warning => warning.Contains("scheduled playlist data", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_FiltersByDateRangeAndCampaign()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var firstScenario = await SeedScenarioAsync();
        var secondScenario = await SeedScenarioAsync(
            campaignName: "Second Campaign",
            date: new DateOnly(2026, 6, 11));

        await SeedProofOfPlayEventAsync(new ProofOfPlayEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = firstScenario.OrganizationId,
            ScreenId = firstScenario.ScreenId,
            PlaylistId = firstScenario.PlaylistId,
            PlaylistItemId = firstScenario.PlaylistItemId,
            CampaignId = firstScenario.CampaignId,
            CreativeId = firstScenario.CreativeId,
            PlayedAt = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            DurationSeconds = 15,
            CreatedAt = DateTime.UtcNow
        });

        await SeedProofOfPlayEventAsync(new ProofOfPlayEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = secondScenario.OrganizationId,
            ScreenId = secondScenario.ScreenId,
            PlaylistId = secondScenario.PlaylistId,
            PlaylistItemId = secondScenario.PlaylistItemId,
            CampaignId = secondScenario.CampaignId,
            CreativeId = secondScenario.CreativeId,
            PlayedAt = new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            DurationSeconds = 15,
            CreatedAt = DateTime.UtcNow
        });

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={firstScenario.CampaignId}&from=2026-06-10&to=2026-06-10");

        var report = await response.Content.ReadFromJsonAsync<EstimatedProofOfPlayReportDto>();
        Assert.NotNull(report);
        var item = Assert.Single(report!.Items);
        Assert.Equal("2026-06-10", item.Date);
        Assert.Equal(firstScenario.CampaignId, report.CampaignId);
        Assert.DoesNotContain(report.Items, row => row.CreativeId == secondScenario.CreativeId);
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_ReturnsAudienceWarningWhenBuildingAudienceMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync(estimatedDailyAudience: 0);

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={scenario.CampaignId}&from=2026-06-10&to=2026-06-10");

        var report = await response.Content.ReadFromJsonAsync<EstimatedProofOfPlayReportDto>();
        Assert.NotNull(report);
        Assert.Contains(report!.Warnings, warning => warning.Contains("missing estimated daily audience", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, report.EstimatedAudience);
    }

    [Fact]
    public async Task GetEstimatedProofOfPlay_ReturnsEmptyReportWhenNoDataExistsInRange()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync(date: new DateOnly(2026, 6, 10));

        var response = await client.GetAsync($"/api/reports/estimated-proof-of-play?campaignId={scenario.CampaignId}&from=2026-06-12&to=2026-06-12");

        var report = await response.Content.ReadFromJsonAsync<EstimatedProofOfPlayReportDto>();
        Assert.NotNull(report);
        Assert.Empty(report!.Items);
        Assert.Equal(0, report.TotalScheduledPlays);
        Assert.Equal(0, report.TotalReportedPlays);
    }

    private async Task<ScenarioIds> SeedScenarioAsync(
        string campaignName = "June Campaign",
        int estimatedDailyAudience = 120,
        DateOnly? date = null)
    {
        var organizationId = Guid.NewGuid();
        var advertiserId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var screenId = Guid.NewGuid();
        var playlistId = Guid.NewGuid();
        var playlistItemId = Guid.NewGuid();
        var effectiveDate = date ?? new DateOnly(2026, 6, 10);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Organizations.Add(new Organization
        {
            Id = organizationId,
            Name = "Org",
            Slug = $"org-{organizationId:N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Advertisers.Add(new Advertiser
        {
            Id = advertiserId,
            OrganizationId = organizationId,
            Name = "Advertiser A",
            LegalName = "Advertiser A Ltd",
            TaxId = "PT123",
            ContactName = "Ana",
            ContactEmail = "ana@example.com",
            Phone = "+351000000000",
            Status = AdvertiserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Campaigns.Add(new Campaign
        {
            Id = campaignId,
            OrganizationId = organizationId,
            AdvertiserId = advertiserId,
            Name = campaignName,
            StartDate = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EndDate = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Buildings.Add(new Building
        {
            Id = buildingId,
            OrganizationId = organizationId,
            Name = "Tower One",
            Address = "Rua 1",
            City = "Lisbon",
            Country = "Portugal",
            PostalCode = "1000-000",
            BuildingType = BuildingType.Commercial,
            EstimatedDailyAudience = estimatedDailyAudience,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Screens.Add(new Screen
        {
            Id = screenId,
            OrganizationId = organizationId,
            BuildingId = buildingId,
            Name = "Lift Screen 1",
            ExternalCode = "LS-1",
            ResolutionWidth = 1080,
            ResolutionHeight = 1920,
            Orientation = ScreenOrientation.Portrait,
            Status = ScreenStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Creatives.Add(new Creative
        {
            Id = creativeId,
            OrganizationId = organizationId,
            AdvertiserId = advertiserId,
            Name = "Creative One",
            MediaUrl = "https://example.com/creative-1.jpg",
            MediaType = MediaType.Image,
            DurationSeconds = 15,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.CampaignCreatives.Add(new CampaignCreative
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            CreativeId = creativeId,
            CreatedAt = DateTime.UtcNow
        });

        context.DailyPlaylists.Add(new DailyPlaylist
        {
            Id = playlistId,
            OrganizationId = organizationId,
            ScreenId = screenId,
            Date = effectiveDate,
            Version = 1,
            Status = DailyPlaylistStatus.Published,
            GeneratedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items =
            [
                new DailyPlaylistItem
                {
                    Id = playlistItemId,
                    DailyPlaylistId = playlistId,
                    CampaignId = campaignId,
                    CreativeId = creativeId,
                    Order = 1,
                    DurationSeconds = 15,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        });

        await context.SaveChangesAsync();

        return new ScenarioIds(
            organizationId,
            campaignId,
            creativeId,
            screenId,
            playlistId,
            playlistItemId);
    }

    private async Task SeedProofOfPlayEventAsync(ProofOfPlayEvent proofOfPlayEvent)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.ProofOfPlayEvents.Add(proofOfPlayEvent);
        await context.SaveChangesAsync();
    }

    private sealed record ScenarioIds(
        Guid OrganizationId,
        Guid CampaignId,
        Guid CreativeId,
        Guid ScreenId,
        Guid PlaylistId,
        Guid PlaylistItemId);

    private sealed record EstimatedProofOfPlayReportDto(
        Guid CampaignId,
        string CampaignName,
        Guid AdvertiserId,
        string AdvertiserName,
        string DateFrom,
        string DateTo,
        int TotalScheduledPlays,
        int TotalReportedPlays,
        long EstimatedAudience,
        long EstimatedImpressions,
        int ScreensCount,
        int BuildingsCount,
        IReadOnlyList<string> Cities,
        IReadOnlyList<EstimatedProofOfPlayItemDto> Items,
        IReadOnlyList<string> Assumptions,
        IReadOnlyList<string> Warnings);

    private sealed record EstimatedProofOfPlayItemDto(
        string Date,
        Guid ScreenId,
        string ScreenName,
        Guid BuildingId,
        string BuildingName,
        string City,
        Guid CreativeId,
        string CreativeName,
        int ScheduledPlays,
        int ReportedPlays,
        long EstimatedAudience,
        long EstimatedImpressions);
}
