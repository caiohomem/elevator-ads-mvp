using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Infrastructure.Persistence;
using ElevatorAds.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.Reports;

public class AdvertiserCampaignReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AdvertiserCampaignReportEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GenerateReport_ReturnsOk_WithSummaryAndBreakdown()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();

        await SeedProofOfPlayEventAsync(new ProofOfPlayEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = scenario.OrganizationId,
            ScreenId = scenario.ScreenId,
            PlaylistId = scenario.PlaylistId,
            PlaylistItemId = scenario.PlaylistItemId,
            CampaignId = scenario.CampaignId,
            CreativeId = scenario.CreativeId,
            PlayedAt = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
            DurationSeconds = 15,
            CreatedAt = DateTime.UtcNow
        });

        var response = await client.GetAsync($"/api/advertisers/{scenario.AdvertiserId}/campaign-reports/{scenario.CampaignId}?from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AdvertiserCampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal("June Campaign", report!.CampaignName);
        Assert.Equal("Active", report.Status);
        Assert.Equal(1, report.TotalPlays);
        Assert.Equal(1, report.TotalScheduledPlays);
        Assert.Equal(1, report.TotalReportedPlays);
        Assert.Single(report.Creatives);
        Assert.Single(report.DailyBreakdown);
        Assert.Equal(1, report.Creatives[0].TotalPlays);
        Assert.Equal(1, report.DailyBreakdown[0].TotalPlays);
    }

    [Fact]
    public async Task GenerateReport_CampaignNotBelongingToAdvertiser_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();

        var response = await client.GetAsync($"/api/advertisers/{Guid.NewGuid()}/campaign-reports/{scenario.CampaignId}?from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateReport_CampaignNotFound_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var advertiserId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/advertisers/{advertiserId}/campaign-reports/{Guid.NewGuid()}?from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateReport_MissingDates_ReturnsUnprocessableEntity()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();

        var response = await client.GetAsync($"/api/advertisers/{scenario.AdvertiserId}/campaign-reports/{scenario.CampaignId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GenerateReport_FiltersByDateRange()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync(date: new DateOnly(2026, 6, 10));

        var response = await client.GetAsync($"/api/advertisers/{scenario.AdvertiserId}/campaign-reports/{scenario.CampaignId}?from=2026-06-11&to=2026-06-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AdvertiserCampaignReportDto>();
        Assert.NotNull(report);
        Assert.Empty(report!.DailyBreakdown);
        Assert.Empty(report.Creatives);
        Assert.Equal(0, report.TotalPlays);
        Assert.Equal(0, report.TotalScheduledPlays);
        Assert.Equal(0, report.TotalReportedPlays);
    }

    [Fact]
    public async Task GenerateReport_CreativeSummaryAggregation()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync(
            extraPlaylistItems:
            [
                new PlaylistSeedItem(Guid.NewGuid(), "Creative Two", MediaType.Video, 30, Order: 2)
            ]);

        var response = await client.GetAsync($"/api/advertisers/{scenario.AdvertiserId}/campaign-reports/{scenario.CampaignId}?from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AdvertiserCampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.Creatives.Count);
        Assert.Contains(report.Creatives, item => item.CreativeName == "Creative One" && item.TotalPlays == 1);
        Assert.Contains(report.Creatives, item => item.CreativeName == "Creative Two" && item.TotalPlays == 1);
    }

    [Fact]
    public async Task GenerateReport_DailyBreakdownAggregation()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var firstDay = await SeedScenarioAsync(date: new DateOnly(2026, 6, 10));
        await SeedScenarioAsync(
            organizationId: firstDay.OrganizationId,
            advertiserId: firstDay.AdvertiserId,
            campaignId: firstDay.CampaignId,
            creativeId: Guid.NewGuid(),
            date: new DateOnly(2026, 6, 11),
            campaignName: "June Campaign");

        var response = await client.GetAsync($"/api/advertisers/{firstDay.AdvertiserId}/campaign-reports/{firstDay.CampaignId}?from=2026-06-10&to=2026-06-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AdvertiserCampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.DailyBreakdown.Count);
        Assert.Contains(report.DailyBreakdown, item => item.Date == "2026-06-10" && item.TotalPlays == 1);
        Assert.Contains(report.DailyBreakdown, item => item.Date == "2026-06-11" && item.TotalPlays == 1);
    }

    [Fact]
    public async Task GenerateReport_IncludesAssumptionsAndWarnings()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var scenario = await SeedScenarioAsync();

        var response = await client.GetAsync($"/api/advertisers/{scenario.AdvertiserId}/campaign-reports/{scenario.CampaignId}?from=2026-06-10&to=2026-06-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AdvertiserCampaignReportDto>();
        Assert.NotNull(report);
        Assert.NotEmpty(report!.Assumptions);
        Assert.Contains(report.Warnings, warning => warning.Contains("scheduled playlist data", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ScenarioIds> SeedScenarioAsync(
        Guid? organizationId = null,
        Guid? advertiserId = null,
        Guid? campaignId = null,
        Guid? creativeId = null,
        string campaignName = "June Campaign",
        int estimatedDailyAudience = 120,
        DateOnly? date = null,
        IReadOnlyList<PlaylistSeedItem>? extraPlaylistItems = null)
    {
        var effectiveOrganizationId = organizationId ?? Guid.NewGuid();
        var effectiveAdvertiserId = advertiserId ?? Guid.NewGuid();
        var effectiveCampaignId = campaignId ?? Guid.NewGuid();
        var primaryCreativeId = creativeId ?? Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var screenId = Guid.NewGuid();
        var playlistId = Guid.NewGuid();
        var playlistItemId = Guid.NewGuid();
        var effectiveDate = date ?? new DateOnly(2026, 6, 10);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!context.Organizations.Any(item => item.Id == effectiveOrganizationId))
        {
            context.Organizations.Add(new Organization
            {
                Id = effectiveOrganizationId,
                Name = "Org",
                Slug = $"org-{effectiveOrganizationId:N}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!context.Advertisers.Any(item => item.Id == effectiveAdvertiserId))
        {
            context.Advertisers.Add(new Advertiser
            {
                Id = effectiveAdvertiserId,
                OrganizationId = effectiveOrganizationId,
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
        }

        if (!context.Campaigns.Any(item => item.Id == effectiveCampaignId))
        {
            context.Campaigns.Add(new Campaign
            {
                Id = effectiveCampaignId,
                OrganizationId = effectiveOrganizationId,
                AdvertiserId = effectiveAdvertiserId,
                Name = campaignName,
                StartDate = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                EndDate = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Status = CampaignStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        context.Buildings.Add(new Building
        {
            Id = buildingId,
            OrganizationId = effectiveOrganizationId,
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
            OrganizationId = effectiveOrganizationId,
            BuildingId = buildingId,
            Name = "Lift Screen 1",
            ExternalCode = $"LS-{screenId:N}",
            ResolutionWidth = 1080,
            ResolutionHeight = 1920,
            Orientation = ScreenOrientation.Portrait,
            Status = ScreenStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        if (!context.Creatives.Any(item => item.Id == primaryCreativeId))
        {
            context.Creatives.Add(new Creative
            {
                Id = primaryCreativeId,
                OrganizationId = effectiveOrganizationId,
                AdvertiserId = effectiveAdvertiserId,
                Name = "Creative One",
                MediaUrl = "https://example.com/creative-1.jpg",
                MediaType = MediaType.Image,
                DurationSeconds = 15,
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!context.CampaignCreatives.Any(item => item.CampaignId == effectiveCampaignId && item.CreativeId == primaryCreativeId))
        {
            context.CampaignCreatives.Add(new CampaignCreative
            {
                Id = Guid.NewGuid(),
                CampaignId = effectiveCampaignId,
                CreativeId = primaryCreativeId,
                CreatedAt = DateTime.UtcNow
            });
        }

        var playlistItems = new List<DailyPlaylistItem>
        {
            new()
            {
                Id = playlistItemId,
                DailyPlaylistId = playlistId,
                CampaignId = effectiveCampaignId,
                CreativeId = primaryCreativeId,
                Order = 1,
                DurationSeconds = 15,
                CreatedAt = DateTime.UtcNow
            }
        };

        if (extraPlaylistItems is not null)
        {
            foreach (var item in extraPlaylistItems)
            {
                if (!context.Creatives.Any(existing => existing.Id == item.CreativeId))
                {
                    context.Creatives.Add(new Creative
                    {
                        Id = item.CreativeId,
                        OrganizationId = effectiveOrganizationId,
                        AdvertiserId = effectiveAdvertiserId,
                        Name = item.Name,
                        MediaUrl = $"https://example.com/{item.CreativeId:N}.mp4",
                        MediaType = item.MediaType,
                        DurationSeconds = item.DurationSeconds,
                        ApprovalStatus = ApprovalStatus.Approved,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (!context.CampaignCreatives.Any(existing => existing.CampaignId == effectiveCampaignId && existing.CreativeId == item.CreativeId))
                {
                    context.CampaignCreatives.Add(new CampaignCreative
                    {
                        Id = Guid.NewGuid(),
                        CampaignId = effectiveCampaignId,
                        CreativeId = item.CreativeId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                playlistItems.Add(new DailyPlaylistItem
                {
                    Id = Guid.NewGuid(),
                    DailyPlaylistId = playlistId,
                    CampaignId = effectiveCampaignId,
                    CreativeId = item.CreativeId,
                    Order = item.Order,
                    DurationSeconds = item.DurationSeconds,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        context.DailyPlaylists.Add(new DailyPlaylist
        {
            Id = playlistId,
            OrganizationId = effectiveOrganizationId,
            ScreenId = screenId,
            Date = effectiveDate,
            Version = 1,
            Status = DailyPlaylistStatus.Published,
            GeneratedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = playlistItems
        });

        await context.SaveChangesAsync();

        return new ScenarioIds(
            effectiveOrganizationId,
            effectiveAdvertiserId,
            effectiveCampaignId,
            primaryCreativeId,
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

    private sealed record PlaylistSeedItem(Guid CreativeId, string Name, MediaType MediaType, int DurationSeconds, int Order);

    private sealed record ScenarioIds(
        Guid OrganizationId,
        Guid AdvertiserId,
        Guid CampaignId,
        Guid CreativeId,
        Guid ScreenId,
        Guid PlaylistId,
        Guid PlaylistItemId);

    private sealed record AdvertiserCampaignReportDto(
        Guid AdvertiserId,
        string AdvertiserName,
        Guid CampaignId,
        string CampaignName,
        string DateFrom,
        string DateTo,
        string Status,
        int TotalPlays,
        int TotalScheduledPlays,
        int TotalReportedPlays,
        long EstimatedAudience,
        long EstimatedImpressions,
        int ScreensCount,
        int BuildingsCount,
        IReadOnlyList<string> Cities,
        IReadOnlyList<AdvertiserCampaignCreativeSummaryDto> Creatives,
        IReadOnlyList<AdvertiserCampaignDailyBreakdownDto> DailyBreakdown,
        IReadOnlyList<string> Assumptions,
        IReadOnlyList<string> Warnings);

    private sealed record AdvertiserCampaignCreativeSummaryDto(
        Guid CreativeId,
        string CreativeName,
        string MediaType,
        int DurationSeconds,
        int TotalPlays,
        long EstimatedImpressions);

    private sealed record AdvertiserCampaignDailyBreakdownDto(
        string Date,
        int TotalPlays,
        long EstimatedAudience,
        long EstimatedImpressions,
        int ScreensCount,
        int BuildingsCount);
}
