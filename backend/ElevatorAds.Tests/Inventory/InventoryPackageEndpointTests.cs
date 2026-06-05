using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Common;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Inventory;

public sealed class InventoryPackageEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public InventoryPackageEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostInventoryPackage_CreatesPackage()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var building = await CreateBuildingAsync(client);
        var screen = await CreateScreenAsync(client, building.Id);

        var request = new CreateInventoryPackageRequest(
            "Lisbon Corporate Buildings",
            "Premium office inventory",
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            [screen.Id],
            [building.Id],
            12.5m,
            "Active");

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<InventoryPackageDto>();
        Assert.NotNull(created);
        Assert.Equal(request.Name, created!.Name);
        Assert.Equal(request.BaseCpm, created.BaseCpm);
        Assert.Equal("Active", created.Status);
    }

    [Fact]
    public async Task GetInventoryPackages_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        await CreateInventoryPackageAsync(client, "Alpha Package");
        var inactive = await CreateInventoryPackageAsync(client, "Beta Package", status: "Inactive");

        var response = await client.GetAsync("/api/inventory-packages?page=1&pageSize=10&status=Inactive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<InventoryPackageDto>>();
        Assert.NotNull(page);
        Assert.Single(page!.Items);
        Assert.Equal(inactive.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task GetInventoryPackage_ById_ReturnsPackage()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var created = await CreateInventoryPackageAsync(client);

        var response = await client.GetAsync($"/api/inventory-packages/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<InventoryPackageDto>();
        Assert.NotNull(item);
        Assert.Equal(created.Id, item!.Id);
    }

    [Fact]
    public async Task PutInventoryPackage_UpdatesPackage()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var created = await CreateInventoryPackageAsync(client);

        var request = new UpdateInventoryPackageRequest(
            "Updated Package",
            "Updated description",
            ["Porto"],
            ["Commercial"],
            ["Landscape"],
            [],
            [],
            18.75m,
            "Inactive");

        var response = await client.PutAsJsonAsync($"/api/inventory-packages/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<InventoryPackageDto>();
        Assert.NotNull(item);
        Assert.Equal("Updated Package", item!.Name);
        Assert.Equal("Inactive", item.Status);
        Assert.Equal("Porto", item.Cities[0]);
    }

    [Fact]
    public async Task DeleteInventoryPackage_RemovesPackage()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var created = await CreateInventoryPackageAsync(client);

        var response = await client.DeleteAsync($"/api/inventory-packages/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetchResponse = await client.GetAsync($"/api/inventory-packages/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetchResponse.StatusCode);
    }

    [Fact]
    public async Task GetInventoryPackageScreens_MatchesExplicitScreenIds()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var building = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate");
        var selectedScreen = await CreateScreenAsync(client, building.Id, name: "Selected Screen", orientation: "Portrait");
        await CreateScreenAsync(client, building.Id, name: "Ignored Screen", orientation: "Landscape");

        var package = await CreateInventoryPackageAsync(
            client,
            cities: ["Porto"],
            buildingTypes: ["Residential"],
            screenOrientations: ["Portrait"],
            screenIds: [selectedScreen.Id]);

        var response = await client.GetAsync($"/api/inventory-packages/{package.Id}/screens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screens = await response.Content.ReadFromJsonAsync<List<ScreenDto>>();
        Assert.NotNull(screens);
        Assert.Single(screens!);
        Assert.Equal(selectedScreen.Id, screens[0].Id);
    }

    [Fact]
    public async Task GetInventoryPackageScreens_MatchesBuildingIds()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var includedBuilding = await CreateBuildingAsync(client, name: "Included Building");
        var excludedBuilding = await CreateBuildingAsync(client, name: "Excluded Building", city: "Porto");
        var includedScreen = await CreateScreenAsync(client, includedBuilding.Id, name: "Included");
        await CreateScreenAsync(client, excludedBuilding.Id, name: "Excluded");

        var package = await CreateInventoryPackageAsync(
            client,
            cities: ["Porto"],
            buildingTypes: ["Residential"],
            screenOrientations: ["Landscape"],
            buildingIds: [includedBuilding.Id]);

        var response = await client.GetAsync($"/api/inventory-packages/{package.Id}/screens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screens = await response.Content.ReadFromJsonAsync<List<ScreenDto>>();
        Assert.NotNull(screens);
        Assert.Single(screens!);
        Assert.Equal(includedScreen.Id, screens[0].Id);
    }

    [Fact]
    public async Task GetInventoryPackageScreens_MatchesFilters()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var matchingBuilding = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Corporate");
        var wrongCityBuilding = await CreateBuildingAsync(client, city: "Porto", buildingType: "Corporate");
        var wrongTypeBuilding = await CreateBuildingAsync(client, city: "Lisbon", buildingType: "Residential");
        var matchingScreen = await CreateScreenAsync(client, matchingBuilding.Id, name: "Matching", orientation: "Portrait");
        await CreateScreenAsync(client, wrongCityBuilding.Id, name: "Wrong City", orientation: "Portrait");
        await CreateScreenAsync(client, wrongTypeBuilding.Id, name: "Wrong Type", orientation: "Portrait");
        await CreateScreenAsync(client, matchingBuilding.Id, name: "Wrong Orientation", orientation: "Landscape");

        var package = await CreateInventoryPackageAsync(
            client,
            cities: ["Lisbon"],
            buildingTypes: ["Corporate"],
            screenOrientations: ["Portrait"]);

        var response = await client.GetAsync($"/api/inventory-packages/{package.Id}/screens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var screens = await response.Content.ReadFromJsonAsync<List<ScreenDto>>();
        Assert.NotNull(screens);
        Assert.Single(screens!);
        Assert.Equal(matchingScreen.Id, screens[0].Id);
    }

    [Fact]
    public async Task PostInventoryPackage_NegativeBaseCpm_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(baseCpm: -0.01m);

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostInventoryPackage_InvalidBuildingReference_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(buildingIds: [Guid.NewGuid()]);

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostInventoryPackage_InvalidScreenReference_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(screenIds: [Guid.NewGuid()]);

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostInventoryPackage_InvalidBuildingType_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(buildingTypes: ["Mall"]);

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostInventoryPackage_InvalidScreenOrientation_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(screenOrientations: ["Square"]);

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private static CreateInventoryPackageRequest CreateRequest(
        List<string>? cities = null,
        List<string>? buildingTypes = null,
        List<string>? screenOrientations = null,
        List<Guid>? screenIds = null,
        List<Guid>? buildingIds = null,
        decimal baseCpm = 10m) =>
        new(
            "All Screens Network",
            "Default package",
            cities ?? [],
            buildingTypes ?? [],
            screenOrientations ?? [],
            screenIds ?? [],
            buildingIds ?? [],
            baseCpm,
            "Active");

    private async Task<InventoryPackageDto> CreateInventoryPackageAsync(
        HttpClient client,
        string name = "Inventory Package",
        List<string>? cities = null,
        List<string>? buildingTypes = null,
        List<string>? screenOrientations = null,
        List<Guid>? screenIds = null,
        List<Guid>? buildingIds = null,
        string status = "Active")
    {
        var request = CreateRequest(cities, buildingTypes, screenOrientations, screenIds, buildingIds) with
        {
            Name = name,
            Status = status
        };

        var response = await client.PostAsJsonAsync("/api/inventory-packages", request);
        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<InventoryPackageDto>();
        Assert.NotNull(item);
        return item!;
    }

    private async Task<BuildingDto> CreateBuildingAsync(
        HttpClient client,
        string name = "Building",
        string city = "Lisbon",
        string buildingType = "Corporate")
    {
        var request = new CreateBuildingRequest(
            name,
            "Avenida Central 1",
            city,
            "Portugal",
            "1000-001",
            buildingType,
            1200);

        var response = await client.PostAsJsonAsync("/api/buildings", request);
        response.EnsureSuccessStatusCode();

        var building = await response.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.NotNull(building);
        return building!;
    }

    private async Task<ScreenDto> CreateScreenAsync(
        HttpClient client,
        Guid buildingId,
        string name = "Screen",
        string orientation = "Portrait")
    {
        var request = new CreateScreenRequest(
            buildingId,
            name,
            $"{name.ToLowerInvariant().Replace(' ', '-')}-ext",
            1080,
            1920,
            orientation,
            "Active");

        var response = await client.PostAsJsonAsync("/api/screens", request);
        response.EnsureSuccessStatusCode();

        var screen = await response.Content.ReadFromJsonAsync<ScreenDto>();
        Assert.NotNull(screen);
        return screen!;
    }

    private sealed record CreateInventoryPackageRequest(
        string Name,
        string Description,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        List<Guid> ScreenIds,
        List<Guid> BuildingIds,
        decimal BaseCpm,
        string Status);

    private sealed record UpdateInventoryPackageRequest(
        string Name,
        string Description,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        List<Guid> ScreenIds,
        List<Guid> BuildingIds,
        decimal BaseCpm,
        string Status);

    private sealed record InventoryPackageDto(
        Guid Id,
        string Name,
        string Description,
        IReadOnlyList<string> Cities,
        IReadOnlyList<string> BuildingTypes,
        IReadOnlyList<string> ScreenOrientations,
        IReadOnlyList<Guid> ScreenIds,
        IReadOnlyList<Guid> BuildingIds,
        decimal BaseCpm,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record CreateBuildingRequest(
        string Name,
        string Address,
        string City,
        string Country,
        string PostalCode,
        string BuildingType,
        int EstimatedDailyAudience);

    private sealed record BuildingDto(Guid Id, string Name, string City, string BuildingType);

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
