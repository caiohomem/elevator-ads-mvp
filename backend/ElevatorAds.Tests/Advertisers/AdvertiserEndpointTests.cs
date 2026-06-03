using ElevatorAds.Tests.Infrastructure;
using ElevatorAds.Domain.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Advertisers;

public class AdvertiserEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdvertiserEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostAdvertiser_CreatesAdvertiser()
    {
        var client = CreateClient();
        var request = new CreateAdvertiserRequest(
            "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        Assert.NotEqual(Guid.Empty, advertiser!.Id);
        Assert.Equal(request.Name, advertiser.Name);
        Assert.Equal(request.ContactEmail, advertiser.ContactEmail);
        Assert.Equal(request.Status, advertiser.Status);
    }

    [Fact]
    public async Task GetAdvertisers_ReturnsPagedResult_AndSupportsFiltering()
    {
        var client = CreateClient();
        var alpha = await CreateAdvertiserAsync(client, "Alpha Media", "Active");
        var beta = await CreateAdvertiserAsync(client, "Beta Media", "Inactive");
        var gamma = await CreateAdvertiserAsync(client, "Gamma Media", "Active");

        var pageResponse = await client.GetAsync("/api/advertisers?page=1&pageSize=2");
        var sortedResponse = await client.GetAsync("/api/advertisers?sortBy=name&sortDirection=asc");
        var searchedResponse = await client.GetAsync("/api/advertisers?search=beta");
        var activeResponse = await client.GetAsync("/api/advertisers?status=Active");
        var invalidPageResponse = await client.GetAsync("/api/advertisers?page=0");

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sortedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, searchedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageResponse.StatusCode);

        var page = await pageResponse.Content.ReadFromJsonAsync<PagedResult<AdvertiserDto>>();
        var sorted = await sortedResponse.Content.ReadFromJsonAsync<PagedResult<AdvertiserDto>>();
        var searched = await searchedResponse.Content.ReadFromJsonAsync<PagedResult<AdvertiserDto>>();
        var active = await activeResponse.Content.ReadFromJsonAsync<PagedResult<AdvertiserDto>>();

        Assert.NotNull(page);
        Assert.NotNull(sorted);
        Assert.NotNull(searched);
        Assert.NotNull(active);
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("Alpha Media", sorted!.Items[0].Name);
        Assert.Single(searched!.Items);
        Assert.Equal(beta.Id, searched.Items[0].Id);
        Assert.All(active!.Items, item => Assert.Equal("Active", item.Status));
        Assert.Contains(active.Items, item => item.Id == alpha.Id);
        Assert.Contains(active.Items, item => item.Id == gamma.Id);
    }

    [Fact]
    public async Task GetAdvertiser_ById_ReturnsAdvertiser()
    {
        var client = CreateClient();
        var created = await CreateAdvertiserAsync(client);

        var response = await client.GetAsync($"/api/advertisers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        Assert.Equal(created.Id, advertiser!.Id);
        Assert.Equal(created.Name, advertiser.Name);
    }

    [Fact]
    public async Task PutAdvertiser_UpdatesAdvertiser()
    {
        var client = CreateClient();
        var created = await CreateAdvertiserAsync(client);
        var request = new CreateAdvertiserRequest(
            "Acme Updated",
            "Acme Holdings SA",
            "PT987654321",
            "John Doe",
            "john@acme.test",
            "+351987654321",
            "Inactive");

        var response = await client.PutAsJsonAsync($"/api/advertisers/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        Assert.Equal(request.Name, advertiser!.Name);
        Assert.Equal(request.LegalName, advertiser.LegalName);
        Assert.Equal(request.TaxId, advertiser.TaxId);
        Assert.Equal(request.ContactName, advertiser.ContactName);
        Assert.Equal(request.ContactEmail, advertiser.ContactEmail);
        Assert.Equal(request.Phone, advertiser.Phone);
        Assert.Equal(request.Status, advertiser.Status);
    }

    [Fact]
    public async Task DeleteAdvertiser_DeletesAdvertiser()
    {
        var client = CreateClient();
        var created = await CreateAdvertiserAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/advertisers/{created.Id}");
        var getResponse = await client.GetAsync($"/api/advertisers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostAdvertiser_MissingName_Returns422()
    {
        var client = CreateClient();
        var request = new CreateAdvertiserRequest(
            "",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostAdvertiser_InvalidEmail_Returns422()
    {
        var client = CreateClient();
        var request = new CreateAdvertiserRequest(
            "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "not-an-email",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.WithWebHostBuilder(_ => { }).CreateClient();

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client, string? name = null, string status = "Active")
    {
        var request = new CreateAdvertiserRequest(
            name ?? "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            status);

        var response = await client.PostAsJsonAsync("/api/advertisers", request);
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
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
}
