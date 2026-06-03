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
    private readonly WebApplicationFactory<Program> _factory;

    public DeliveryReportEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetOverview_NoData_ReturnsZeroTotalsAndEmptyGroups()
    {
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
        var (client, factory) = CreateClientWithFactory();
        var date = new DateOnly(2026, 2, 10);
        var screenId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(date.ToDateTime(new TimeOnly(8, 0)), screenId, campaignId, creativeId, 15),
            BuildEvent(date.ToDateTime(new TimeOnly(9, 0)), screenId, campaignId, creativeId, 20));

        var response = await client.GetAsync($"/api/reports/overview?date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<OverviewReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report!.TotalPlays);
        Assert.Equal(35L, report.TotalPlayedSeconds);
        var campaignSummary = Assert.Single(report.ByCampaign);
        Assert.Equal(campaignId, campaignSummary.Id);
        Assert.Equal(2, campaignSummary.Plays);
        Assert.Equal(35L, campaignSummary.PlayedSeconds);
        var screenSummary = Assert.Single(report.ByScreen);
        Assert.Equal(screenId, screenSummary.Id);
        var creativeSummary = Assert.Single(report.ByCreative);
        Assert.Equal(creativeId, creativeSummary.Id);
    }

    [Fact]
    public async Task GetOverview_GroupsByCampaignScreenAndCreative()
    {
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
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/overview");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_InvalidDate_ReturnsUnprocessableEntity()
    {
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/overview?date=not-a-date");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCampaigns_GroupsByCampaign()
    {
        var (client, factory) = CreateClientWithFactory();
        var firstCampaign = Guid.NewGuid();
        var secondCampaign = Guid.NewGuid();
        var screenId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 5, 1, 10, 0, 0), screenId, firstCampaign, creativeId, 10),
            BuildEvent(new DateTime(2026, 5, 2, 10, 0, 0), screenId, firstCampaign, creativeId, 20),
            BuildEvent(new DateTime(2026, 5, 3, 10, 0, 0), screenId, secondCampaign, creativeId, 30));

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-05-01&to=2026-05-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CampaignReportDto>();
        Assert.NotNull(report);
        Assert.Equal("2026-05-01", report!.From);
        Assert.Equal("2026-05-03", report.To);
        Assert.Equal(3, report.TotalPlays);
        Assert.Equal(60L, report.TotalPlayedSeconds);
        Assert.Equal(2, report.Campaigns.Count);
        var firstSummary = report.Campaigns.Single(item => item.Id == firstCampaign);
        Assert.Equal(2, firstSummary.Plays);
        Assert.Equal(30L, firstSummary.PlayedSeconds);
        var secondSummary = report.Campaigns.Single(item => item.Id == secondCampaign);
        Assert.Equal(1, secondSummary.Plays);
        Assert.Equal(30L, secondSummary.PlayedSeconds);
    }

    [Fact]
    public async Task GetCampaigns_FiltersByDateRange()
    {
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
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-06-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCampaigns_ToBeforeFrom_ReturnsUnprocessableEntity()
    {
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/campaigns?from=2026-06-05&to=2026-06-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetScreens_GroupsByScreen()
    {
        var (client, factory) = CreateClientWithFactory();
        var firstScreen = Guid.NewGuid();
        var secondScreen = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var creativeId = Guid.NewGuid();
        await SeedEventsAsync(factory,
            BuildEvent(new DateTime(2026, 7, 1, 10, 0, 0), firstScreen, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 7, 2, 10, 0, 0), firstScreen, campaignId, creativeId, 10),
            BuildEvent(new DateTime(2026, 7, 3, 10, 0, 0), secondScreen, campaignId, creativeId, 20));

        var response = await client.GetAsync("/api/reports/screens?from=2026-07-01&to=2026-07-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ScreenReportDto>();
        Assert.NotNull(report);
        Assert.Equal("2026-07-01", report!.From);
        Assert.Equal("2026-07-03", report.To);
        Assert.Equal(3, report.TotalPlays);
        Assert.Equal(40L, report.TotalPlayedSeconds);
        Assert.Equal(2, report.Screens.Count);
        var firstSummary = report.Screens.Single(item => item.Id == firstScreen);
        Assert.Equal(2, firstSummary.Plays);
        Assert.Equal(20L, firstSummary.PlayedSeconds);
    }

    [Fact]
    public async Task GetScreens_FiltersByDateRange()
    {
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
        var (client, _) = CreateClientWithFactory();

        var response = await client.GetAsync("/api/reports/screens?from=2026-08-01");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private (HttpClient Client, WebApplicationFactory<Program> Factory) CreateClientWithFactory()
    {
        var factory = _factory.WithWebHostBuilder(_ => { });
        return (factory.CreateClient(), factory);
    }

    private static async Task SeedEventsAsync(WebApplicationFactory<Program> factory, params ProofOfPlayEvent[] events)
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

    private sealed record GroupSummaryDto(Guid Id, int Plays, long PlayedSeconds);

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
}
