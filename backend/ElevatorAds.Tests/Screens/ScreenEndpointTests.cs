using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevatorAds.Tests.Screens;

public class ScreenEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScreenEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostScreen_CreatesScreen()
    {
        var client = CreateClient();
        var building = await CreateBuildingAsync(client);
        var request = new CreateScreenRequest(
            building.Id,
            "Lobby Screen",
            "SCR-001",
            1080,
            1920,
            "Portrait",
            "Active");

        var response = await client.PostAsJsonAsync("/api/screens", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        Assert.NotEqual(Guid.Empty, screen!.Id);
        Assert.Equal(request.BuildingId, screen.BuildingId);
        Assert.Equal(request.Name, screen.Name);
        Assert.Null(screen.LastSeenAt);
    }

    [Fact]
    public async Task GetScreens_ReturnsCreatedScreen()
    {
        var client = CreateClient();
        var created = await CreateScreenAsync(client);

        var response = await client.GetAsync("/api/screens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screens = await response.Content.ReadFromJsonAsync<List<ScreenDto>>();
        Assert.NotNull(screens);
        Assert.Contains(screens!, screen => screen.Id == created.Id);
    }

    [Fact]
    public async Task GetScreenById_ReturnsScreen()
    {
        var client = CreateClient();
        var created = await CreateScreenAsync(client);

        var response = await client.GetAsync($"/api/screens/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        Assert.Equal(created.Id, screen!.Id);
        Assert.Equal(created.Name, screen.Name);
    }

    [Fact]
    public async Task PutScreen_UpdatesScreen()
    {
        var client = CreateClient();
        var created = await CreateScreenAsync(client);
        var request = new CreateScreenRequest(
            created.BuildingId,
            "Updated Screen",
            "SCR-002",
            2160,
            3840,
            "Landscape",
            "Maintenance");

        var response = await client.PutAsJsonAsync($"/api/screens/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        Assert.Equal(request.Name, screen!.Name);
        Assert.Equal(request.ExternalCode, screen.ExternalCode);
        Assert.Equal(request.ResolutionWidth, screen.ResolutionWidth);
        Assert.Equal(request.ResolutionHeight, screen.ResolutionHeight);
        Assert.Equal(request.Orientation, screen.Orientation);
        Assert.Equal(request.Status, screen.Status);
    }

    [Fact]
    public async Task DeleteScreen_RemovesScreen()
    {
        var client = CreateClient();
        var created = await CreateScreenAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/screens/{created.Id}");
        var getResponse = await client.GetAsync($"/api/screens/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostStatusCheck_UpdatesLastSeenAt()
    {
        var client = CreateClient();
        var created = await CreateScreenAsync(client);

        var response = await client.PostAsync($"/api/screens/{created.Id}/status-check", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        Assert.NotNull(screen!.LastSeenAt);
        Assert.True(screen.LastSeenAt >= created.CreatedAt);
    }

    [Fact]
    public async Task PostScreen_WithoutBuildingId_ReturnsValidationFailure()
    {
        var client = CreateClient();
        var request = new CreateScreenRequest(
            Guid.Empty,
            "Lobby Screen",
            "SCR-001",
            1080,
            1920,
            "Portrait",
            "Active");

        var response = await client.PostAsJsonAsync("/api/screens", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostScreen_WithInvalidResolution_ReturnsValidationFailure()
    {
        var client = CreateClient();
        var building = await CreateBuildingAsync(client);
        var request = new CreateScreenRequest(
            building.Id,
            "Lobby Screen",
            "SCR-001",
            0,
            1920,
            "Portrait",
            "Active");

        var response = await client.PostAsJsonAsync("/api/screens", request);

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

    private async Task<ScreenDto> CreateScreenAsync(HttpClient client)
    {
        var building = await CreateBuildingAsync(client);
        var request = new CreateScreenRequest(
            building.Id,
            "Lobby Screen",
            "SCR-001",
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
}
