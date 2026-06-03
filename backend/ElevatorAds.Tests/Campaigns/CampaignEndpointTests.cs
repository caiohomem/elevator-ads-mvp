using ElevatorAds.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Campaigns;

public class CampaignEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CampaignEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateCampaign_ReturnsCreated()
    {
        var client = CreateClient();
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        Assert.NotEqual(Guid.Empty, campaign!.Id);
        Assert.Equal(request.AdvertiserId, campaign.AdvertiserId);
        Assert.Equal(request.Name, campaign.Name);
        Assert.Equal(request.StartDate, campaign.StartDate);
        Assert.Equal(request.EndDate, campaign.EndDate);
        Assert.Equal(request.Status, campaign.Status);
        Assert.Equal(request.DailyBudget, campaign.DailyBudget);
        Assert.Equal(request.TotalBudget, campaign.TotalBudget);
        Assert.Equal(request.MaxCpm, campaign.MaxCpm);
    }

    [Fact]
    public async Task ListCampaigns_ReturnsOk()
    {
        var client = CreateClient();
        var created = await CreateCampaignAsync(client);

        var response = await client.GetAsync("/api/campaigns");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignDto>>();
        Assert.NotNull(campaigns);
        Assert.Contains(campaigns!, campaign => campaign.Id == created.Id);
    }

    [Fact]
    public async Task GetCampaignById_ReturnsOk()
    {
        var client = CreateClient();
        var created = await CreateCampaignAsync(client);

        var response = await client.GetAsync($"/api/campaigns/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        Assert.Equal(created.Id, campaign!.Id);
        Assert.Equal(created.Name, campaign.Name);
    }

    [Fact]
    public async Task UpdateCampaign_ReturnsOk()
    {
        var client = CreateClient();
        var created = await CreateCampaignAsync(client);
        var request = new UpdateCampaignRequest(
            "Autumn Push",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            "Active",
            250m,
            5000m,
            12m);

        var response = await client.PutAsJsonAsync($"/api/campaigns/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        Assert.NotNull(campaign);
        Assert.Equal(created.AdvertiserId, campaign!.AdvertiserId);
        Assert.Equal(request.Name, campaign.Name);
        Assert.Equal(request.StartDate, campaign.StartDate);
        Assert.Equal(request.EndDate, campaign.EndDate);
        Assert.Equal(request.Status, campaign.Status);
        Assert.Equal(request.DailyBudget, campaign.DailyBudget);
        Assert.Equal(request.TotalBudget, campaign.TotalBudget);
        Assert.Equal(request.MaxCpm, campaign.MaxCpm);
    }

    [Fact]
    public async Task DeleteCampaign_ReturnsNoContent()
    {
        var client = CreateClient();
        var created = await CreateCampaignAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/campaigns/{created.Id}");
        var getResponse = await client.GetAsync($"/api/campaigns/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCampaign_MissingName_Returns422()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            "",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            "Draft",
            100m,
            1000m,
            8.5m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task CreateCampaign_InvalidDateRange_Returns422()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            "Summer Push",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            "Draft",
            100m,
            1000m,
            8.5m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task CreateCampaign_NegativeBudget_Returns422()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCampaignRequest(
            advertiser.Id,
            "Summer Push",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            "Draft",
            -1m,
            1000m,
            8.5m);

        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.WithWebHostBuilder(_ => { }).CreateClient();

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

    private sealed record UpdateCampaignRequest(
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
}
