using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Campaigns;

public class CampaignCreativeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CampaignCreativeEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AssignApprovedCreativeToCampaign_ReturnsCreated()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var creative = await CreateAndApproveCreativeAsync(client);

        var response = await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var assignment = await response.Content.ReadFromJsonAsync<CampaignCreativeDto>();
        Assert.NotNull(assignment);
        Assert.NotEqual(Guid.Empty, assignment!.Id);
        Assert.Equal(campaign.Id, assignment.CampaignId);
        Assert.Equal(creative.Id, assignment.CreativeId);
    }

    [Fact]
    public async Task ListCampaignCreatives_ReturnsOk()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var creative = await CreateAndApproveCreativeAsync(client);
        await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        var response = await client.GetAsync($"/api/campaigns/{campaign.Id}/creatives");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var assignments = await response.Content.ReadFromJsonAsync<List<CampaignCreativeDto>>();
        Assert.NotNull(assignments);
        Assert.Contains(assignments!, assignment => assignment.CampaignId == campaign.Id && assignment.CreativeId == creative.Id);
    }

    [Fact]
    public async Task RemoveCreativeFromCampaign_ReturnsNoContent()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var creative = await CreateAndApproveCreativeAsync(client);
        await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        var deleteResponse = await client.DeleteAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}");
        var listResponse = await client.GetAsync($"/api/campaigns/{campaign.Id}/creatives");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var assignments = await listResponse.Content.ReadFromJsonAsync<List<CampaignCreativeDto>>();
        Assert.NotNull(assignments);
        Assert.DoesNotContain(assignments!, assignment => assignment.CreativeId == creative.Id);
    }

    [Fact]
    public async Task AssignDuplicateCreativeToCampaign_Returns422()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var creative = await CreateAndApproveCreativeAsync(client);
        await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        var response = await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task AssignNonApprovedCreativeToCampaign_Returns422()
    {
        var client = CreateClient();
        var campaign = await CreateCampaignAsync(client);
        var creative = await CreateCreativeAsync(client);

        var response = await client.PostAsync($"/api/campaigns/{campaign.Id}/creatives/{creative.Id}", null);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task AssignCreativeToMissingCampaign_Returns422Or404()
    {
        var client = CreateClient();
        var creative = await CreateAndApproveCreativeAsync(client);

        var response = await client.PostAsync($"/api/campaigns/{Guid.NewGuid()}/creatives/{creative.Id}", null);

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound });
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

    private async Task<CreativeDto> CreateCreativeAsync(HttpClient client)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            "Lobby Promo",
            "https://cdn.example.com/creative.jpg",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);
        response.EnsureSuccessStatusCode();

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        return creative!;
    }

    private async Task<CreativeDto> CreateAndApproveCreativeAsync(HttpClient client)
    {
        var creative = await CreateCreativeAsync(client);

        var submitResponse = await client.PostAsync($"/api/creatives/{creative.Id}/submit-for-review", null);
        submitResponse.EnsureSuccessStatusCode();

        var approveResponse = await client.PostAsync($"/api/creatives/{creative.Id}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        var approvedCreative = await approveResponse.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(approvedCreative);
        return approvedCreative!;
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

    private sealed record CampaignCreativeDto(
        Guid Id,
        Guid CampaignId,
        Guid CreativeId,
        DateTime CreatedAt);
}
