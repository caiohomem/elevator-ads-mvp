using ElevatorAds.Tests.Infrastructure;
using ElevatorAds.Domain.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Creatives;

public class CreativeEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreativeEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostCreative_CreatesCreative()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            "Lobby Promo",
            "https://cdn.example.com/creative.jpg",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.NotEqual(Guid.Empty, creative!.Id);
        Assert.Equal(request.AdvertiserId, creative.AdvertiserId);
        Assert.Equal(request.Name, creative.Name);
        Assert.Equal(request.MediaUrl, creative.MediaUrl);
        Assert.Equal(request.MediaType, creative.MediaType);
        Assert.Equal(request.DurationSeconds, creative.DurationSeconds);
        Assert.Equal("Draft", creative.ApprovalStatus);
    }

    [Fact]
    public async Task GetCreatives_ReturnsPagedResult_AndSupportsFiltering()
    {
        var client = CreateClient();
        var alpha = await CreateCreativeAsync(client, "Alpha Promo");
        var beta = await CreateCreativeAsync(client, "Beta Promo");
        var gamma = await CreateCreativeAsync(client, "Gamma Promo");
        await client.PostAsync($"/api/creatives/{beta.Id}/submit-for-review", null);
        await client.PostAsync($"/api/creatives/{beta.Id}/approve", null);

        var pageResponse = await client.GetAsync("/api/creatives?page=1&pageSize=2");
        var sortedResponse = await client.GetAsync("/api/creatives?sortBy=name&sortDirection=asc");
        var searchedResponse = await client.GetAsync("/api/creatives?search=gamma");
        var approvedResponse = await client.GetAsync("/api/creatives?status=Approved");
        var invalidPageSizeResponse = await client.GetAsync("/api/creatives?pageSize=101");

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sortedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, searchedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageSizeResponse.StatusCode);

        var page = await pageResponse.Content.ReadFromJsonAsync<PagedResult<CreativeDto>>();
        var sorted = await sortedResponse.Content.ReadFromJsonAsync<PagedResult<CreativeDto>>();
        var searched = await searchedResponse.Content.ReadFromJsonAsync<PagedResult<CreativeDto>>();
        var approved = await approvedResponse.Content.ReadFromJsonAsync<PagedResult<CreativeDto>>();

        Assert.NotNull(page);
        Assert.NotNull(sorted);
        Assert.NotNull(searched);
        Assert.NotNull(approved);
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("Alpha Promo", sorted!.Items[0].Name);
        Assert.Single(searched!.Items);
        Assert.Equal(gamma.Id, searched.Items[0].Id);
        Assert.Single(approved!.Items);
        Assert.Equal(beta.Id, approved.Items[0].Id);
        Assert.Contains(page.Items, item => item.Id == beta.Id);
    }

    [Fact]
    public async Task GetCreativeById_ReturnsCreative()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);

        var response = await client.GetAsync($"/api/creatives/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.Equal(created.Id, creative!.Id);
        Assert.Equal(created.Name, creative.Name);
    }

    [Fact]
    public async Task PutCreative_UpdatesCreative()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);
        var request = new UpdateCreativeRequest(
            "Updated Promo",
            "https://cdn.example.com/creative.mp4",
            "Video",
            30);

        var response = await client.PutAsJsonAsync($"/api/creatives/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.Equal(request.Name, creative!.Name);
        Assert.Equal(request.MediaUrl, creative.MediaUrl);
        Assert.Equal(request.MediaType, creative.MediaType);
        Assert.Equal(request.DurationSeconds, creative.DurationSeconds);
        Assert.Equal(created.AdvertiserId, creative.AdvertiserId);
    }

    [Fact]
    public async Task DeleteCreative_RemovesCreative()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/creatives/{created.Id}");
        var getResponse = await client.GetAsync($"/api/creatives/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitForReview_TransitionsToPendingReview()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);

        var response = await client.PostAsync($"/api/creatives/{created.Id}/submit-for-review", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.Equal("PendingReview", creative!.ApprovalStatus);
    }

    [Fact]
    public async Task ApproveCreative_TransitionsToApproved()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);
        await client.PostAsync($"/api/creatives/{created.Id}/submit-for-review", null);

        var response = await client.PostAsync($"/api/creatives/{created.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.Equal("Approved", creative!.ApprovalStatus);
    }

    [Fact]
    public async Task RejectCreative_TransitionsToRejected()
    {
        var client = CreateClient();
        var created = await CreateCreativeAsync(client);
        await client.PostAsync($"/api/creatives/{created.Id}/submit-for-review", null);

        var response = await client.PostAsync($"/api/creatives/{created.Id}/reject", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        Assert.Equal("Rejected", creative!.ApprovalStatus);
    }

    [Fact]
    public async Task PostCreative_MissingMediaUrl_Returns422()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            "Lobby Promo",
            "",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostCreative_InvalidDuration_Returns422()
    {
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            "Lobby Promo",
            "https://cdn.example.com/creative.jpg",
            "Image",
            0);

        var response = await client.PostAsJsonAsync("/api/creatives", request);

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

    private async Task<CreativeDto> CreateCreativeAsync(HttpClient client, string? name = null)
    {
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateCreativeRequest(
            advertiser.Id,
            name ?? "Lobby Promo",
            "https://cdn.example.com/creative.jpg",
            "Image",
            15);

        var response = await client.PostAsJsonAsync("/api/creatives", request);
        response.EnsureSuccessStatusCode();

        var creative = await response.Content.ReadFromJsonAsync<CreativeDto>();
        Assert.NotNull(creative);
        return creative!;
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

    private sealed record CreateCreativeRequest(
        Guid AdvertiserId,
        string Name,
        string MediaUrl,
        string MediaType,
        int DurationSeconds);

    private sealed record UpdateCreativeRequest(
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
