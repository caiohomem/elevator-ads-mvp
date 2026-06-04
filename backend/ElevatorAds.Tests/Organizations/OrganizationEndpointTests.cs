using ElevatorAds.Tests.Infrastructure;
using ElevatorAds.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ElevatorAds.Tests.Organizations;

public class OrganizationEndpointTests
{
    [Fact]
    public async Task PostOrganization_AsAdmin_CreatesOrganization()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var request = new CreateOrganizationRequest("Acme Networks", "acme-networks", "active");

        var response = await client.PostAsJsonAsync("/api/organizations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.NotNull(org);
        Assert.NotEqual(Guid.Empty, org!.Id);
        Assert.Equal("Acme Networks", org.Name);
        Assert.Equal("acme-networks", org.Slug);
        Assert.Equal("active", org.Status);
    }

    [Fact]
    public async Task PostOrganization_WithoutName_ReturnsValidationFailure()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var request = new CreateOrganizationRequest(string.Empty, "no-name-org", "active");

        var response = await client.PostAsJsonAsync("/api/organizations", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostOrganization_WithoutSlug_ReturnsValidationFailure()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var request = new CreateOrganizationRequest("No Slug Org", string.Empty, "active");

        var response = await client.PostAsJsonAsync("/api/organizations", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostOrganization_WithInvalidSlugCharacters_ReturnsValidationFailure()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var request = new CreateOrganizationRequest("Invalid Slug", "Invalid Slug With Spaces", "active");

        var response = await client.PostAsJsonAsync("/api/organizations", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostOrganization_WithDuplicateSlug_ReturnsValidationFailure()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        var first = await client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("First Org", "shared-slug", "active"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Second Org", "shared-slug", "active"));

        Assert.Equal((HttpStatusCode)422, second.StatusCode);
    }

    [Fact]
    public async Task PostOrganization_AsOperator_ReturnsForbidden()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueOperatorToken());
        var request = new CreateOrganizationRequest("Acme Networks", "acme-networks", "active");

        var response = await client.PostAsJsonAsync("/api/organizations", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrganizations_AsViewer_ReturnsPagedResult()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        await client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest("Alpha", "alpha", "active"));
        await client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest("Bravo", "bravo", "active"));
        await client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest("Charlie", "charlie", "active"));

        var viewerClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueViewerToken());
        var response = await viewerClient.GetAsync("/api/organizations?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<OrganizationDto>>();
        Assert.NotNull(paged);
        Assert.Equal(2, paged!.PageSize);
        Assert.Equal(2, paged.Items.Count);
        // 3 created by this test + 1 default org seeded by DatabaseSeeder on startup
        Assert.Equal(4, paged.TotalItems);
    }

    [Fact]
    public async Task GetOrganizationById_ReturnsOrganization()
    {
        var factory = new TestWebApplicationFactory();
        var adminClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var createResponse = await adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Solo", "solo-org", "active"));
        var created = (await createResponse.Content.ReadFromJsonAsync<OrganizationDto>())!;

        var viewerClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueViewerToken());
        var response = await viewerClient.GetAsync($"/api/organizations/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal(created.Id, org!.Id);
    }

    [Fact]
    public async Task PutOrganization_AsAdmin_UpdatesOrganization()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var createResponse = await client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Old Name", "old-slug", "active"));
        var created = (await createResponse.Content.ReadFromJsonAsync<OrganizationDto>())!;

        var response = await client.PutAsJsonAsync($"/api/organizations/{created.Id}",
            new UpdateOrganizationRequest("New Name", "new-slug", "inactive"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var org = (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
        Assert.Equal("New Name", org.Name);
        Assert.Equal("new-slug", org.Slug);
        Assert.Equal("inactive", org.Status);
    }

    [Fact]
    public async Task DeleteOrganization_AsAdmin_RemovesOrganization()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        var createResponse = await client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Doomed", "doomed-org", "active"));
        var created = (await createResponse.Content.ReadFromJsonAsync<OrganizationDto>())!;

        var deleteResponse = await client.DeleteAsync($"/api/organizations/{created.Id}");
        var getResponse = await client.GetAsync($"/api/organizations/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetOrganizationBySlug_ReturnsOrganization()
    {
        var factory = new TestWebApplicationFactory();
        var adminClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());
        await adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("By Slug", "by-slug", "active"));

        var viewerClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueViewerToken());
        var response = await viewerClient.GetAsync("/api/organizations/by-slug/by-slug");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal("by-slug", org!.Slug);
    }

    [Fact]
    public async Task Buildings_FilteredByOrganization_ReturnOnlyTenantBuildings()
    {
        var factory = new TestWebApplicationFactory();
        var adminClient = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        var orgACreate = await adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Tenant A", "tenant-a", "active"));
        var orgBCreate = await adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest("Tenant B", "tenant-b", "active"));
        var orgA = (await orgACreate.Content.ReadFromJsonAsync<OrganizationDto>())!;
        var orgB = (await orgBCreate.Content.ReadFromJsonAsync<OrganizationDto>())!;

        // Create three buildings, all via HTTP. They will be assigned to the default organization
        // because the production code uses a default-tenant fallback. To test true scoping,
        // we manipulate the DB directly via the test factory.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElevatorAds.Infrastructure.Persistence.AppDbContext>();
        context.Buildings.Add(new ElevatorAds.Domain.Entities.Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgA.Id,
            Name = "A1",
            Address = "A1 St",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000",
            BuildingType = ElevatorAds.Domain.Enums.BuildingType.Corporate,
            EstimatedDailyAudience = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.Buildings.Add(new ElevatorAds.Domain.Entities.Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgA.Id,
            Name = "A2",
            Address = "A2 St",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000",
            BuildingType = ElevatorAds.Domain.Enums.BuildingType.Corporate,
            EstimatedDailyAudience = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.Buildings.Add(new ElevatorAds.Domain.Entities.Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgB.Id,
            Name = "B1",
            Address = "B1 St",
            City = "Porto",
            Country = "PT",
            PostalCode = "2000",
            BuildingType = ElevatorAds.Domain.Enums.BuildingType.Corporate,
            EstimatedDailyAudience = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var orgARepository = new ElevatorAds.Infrastructure.Repositories.EfBuildingRepository(context);
        var orgABuildings = (await orgARepository.GetByOrganizationAsync(orgA.Id)).ToList();
        var orgBBuildings = (await orgBRepository(orgB.Id, context)).ToList();

        Assert.Equal(2, orgABuildings.Count);
        Assert.All(orgABuildings, b => Assert.Equal(orgA.Id, b.OrganizationId));

        Assert.Single(orgBBuildings);
        Assert.Equal(orgB.Id, orgBBuildings[0].OrganizationId);
    }

    private static async Task<List<ElevatorAds.Domain.Entities.Building>> orgBRepository(Guid organizationId, ElevatorAds.Infrastructure.Persistence.AppDbContext context)
    {
        var repo = new ElevatorAds.Infrastructure.Repositories.EfBuildingRepository(context);
        var list = await repo.GetByOrganizationAsync(organizationId);
        return list.ToList();
    }

    private sealed record CreateOrganizationRequest(string Name, string Slug, string? Status);
    private sealed record UpdateOrganizationRequest(string Name, string Slug, string? Status);
    private sealed record OrganizationDto(Guid Id, string Name, string Slug, string Status, DateTime CreatedAt, DateTime UpdatedAt);
}
