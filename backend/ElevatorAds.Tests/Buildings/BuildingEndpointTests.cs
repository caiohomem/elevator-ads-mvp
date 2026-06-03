using ElevatorAds.Tests.Infrastructure;
using ElevatorAds.Domain.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Buildings;

public class BuildingEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BuildingEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostBuilding_CreatesBuilding()
    {
        var client = _factory.CreateClient();
        var request = new CreateBuildingRequest(
            "Tower One",
            "123 Main St",
            "Lisbon",
            "Portugal",
            "1000-001",
            "Corporate",
            500);

        var response = await client.PostAsJsonAsync("/api/buildings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        Assert.NotEqual(Guid.Empty, building!.Id);
        Assert.Equal(request.Name, building.Name);
    }

    [Fact]
    public async Task GetBuildings_ReturnsPagedResult_AndSupportsSortingAndValidation()
    {
        var client = CreateClient();
        var alpha = await CreateBuildingAsync(client, "Alpha Tower", "Lisbon");
        var bravo = await CreateBuildingAsync(client, "Bravo Tower", "Porto");
        var charlie = await CreateBuildingAsync(client, "Charlie Tower", "Braga");

        var page1Response = await client.GetAsync("/api/buildings?page=1&pageSize=2");
        var page2Response = await client.GetAsync("/api/buildings?page=2&pageSize=2");
        var sortedResponse = await client.GetAsync("/api/buildings?sortBy=name&sortDirection=asc");
        var searchedResponse = await client.GetAsync("/api/buildings?search=bravo");
        var invalidPageResponse = await client.GetAsync("/api/buildings?page=0");
        var invalidPageSizeResponse = await client.GetAsync("/api/buildings?pageSize=101");

        Assert.Equal(HttpStatusCode.OK, page1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sortedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, searchedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageSizeResponse.StatusCode);

        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<BuildingDto>>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<BuildingDto>>();
        var sorted = await sortedResponse.Content.ReadFromJsonAsync<PagedResult<BuildingDto>>();
        var searched = await searchedResponse.Content.ReadFromJsonAsync<PagedResult<BuildingDto>>();

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.NotNull(sorted);
        Assert.NotNull(searched);
        Assert.Equal(1, page1!.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Equal(3, page1.TotalItems);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2!.Page);
        Assert.Equal(1, page2.Items.Count);
        Assert.Equal("Alpha Tower", sorted!.Items[0].Name);
        Assert.Single(searched!.Items);
        Assert.Equal(bravo.Id, searched.Items[0].Id);
        Assert.Contains(page1.Items, item => item.Id == bravo.Id);
        Assert.Contains(page1.Items, item => item.Id == charlie.Id);
        Assert.Contains(page2.Items, item => item.Id == alpha.Id);
    }

    [Fact]
    public async Task GetBuildingById_ReturnsBuilding()
    {
        var client = CreateClient();
        var created = await CreateBuildingAsync(client);

        var response = await client.GetAsync($"/api/buildings/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        Assert.Equal(created.Id, building!.Id);
        Assert.Equal(created.Name, building.Name);
    }

    [Fact]
    public async Task PutBuilding_UpdatesBuilding()
    {
        var client = CreateClient();
        var created = await CreateBuildingAsync(client);
        var request = new CreateBuildingRequest(
            "Tower Two",
            "456 Updated Ave",
            "Porto",
            "Portugal",
            "4000-001",
            "MixedUse",
            900);

        var response = await client.PutAsJsonAsync($"/api/buildings/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        Assert.Equal(request.Name, building!.Name);
        Assert.Equal(request.City, building.City);
        Assert.Equal(request.BuildingType, building.BuildingType);
        Assert.Equal(request.EstimatedDailyAudience, building.EstimatedDailyAudience);
    }

    [Fact]
    public async Task DeleteBuilding_RemovesBuilding()
    {
        var client = CreateClient();
        var created = await CreateBuildingAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/buildings/{created.Id}");
        var getResponse = await client.GetAsync($"/api/buildings/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostBuilding_WithoutName_ReturnsValidationFailure()
    {
        var client = CreateClient();
        var request = new CreateBuildingRequest(
            "",
            "123 Main St",
            "Lisbon",
            "Portugal",
            "1000-001",
            "Corporate",
            500);

        var response = await client.PostAsJsonAsync("/api/buildings", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostBuilding_WithNegativeAudience_ReturnsValidationFailure()
    {
        var client = CreateClient();
        var request = new CreateBuildingRequest(
            "Tower One",
            "123 Main St",
            "Lisbon",
            "Portugal",
            "1000-001",
            "Corporate",
            -1);

        var response = await client.PostAsJsonAsync("/api/buildings", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.WithWebHostBuilder(_ => { }).CreateClient();

    private async Task<BuildingDto> CreateBuildingAsync(HttpClient client, string? name = null, string? city = null)
    {
        var request = new CreateBuildingRequest(
            name ?? "Tower One",
            "123 Main St",
            city ?? "Lisbon",
            "Portugal",
            "1000-001",
            "Corporate",
            500);

        var response = await client.PostAsJsonAsync("/api/buildings", request);
        response.EnsureSuccessStatusCode();

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        return building!;
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
}
