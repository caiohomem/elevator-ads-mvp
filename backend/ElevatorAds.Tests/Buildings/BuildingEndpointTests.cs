using ElevatorAds.Tests.Infrastructure;
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
    public async Task GetBuildings_ReturnsCreatedBuilding()
    {
        var client = CreateClient();
        var created = await CreateBuildingAsync(client);

        var response = await client.GetAsync("/api/buildings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var buildings = await response.Content.ReadFromJsonAsync<List<BuildingDto>>();
        Assert.NotNull(buildings);
        Assert.Contains(buildings!, building => building.Id == created.Id);
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

    private async Task<BuildingDto> CreateBuildingAsync(HttpClient client)
    {
        var request = new CreateBuildingRequest(
            "Tower One",
            "123 Main St",
            "Lisbon",
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
