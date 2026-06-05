using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Application.Auth;
using ElevatorAds.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Auth;

public class AuthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await _factory.ResetDatabaseAsync();
        await SeedUserAsync("admin@test", "Password1!", Domain.Enums.UserRole.Admin);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin@test", password = "Password1!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Admin", body.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _factory.ResetDatabaseAsync();
        await SeedUserAsync("operator@test", "Password1!", Domain.Enums.UserRole.Operator);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "operator@test", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "missing@test", password = "Password1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WriteEndpoint_WithoutToken_Returns401()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/buildings", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WriteEndpoint_WithViewerToken_Returns403()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueViewerToken());

        var response = await client.PostAsJsonAsync("/api/buildings", new
        {
            name = "Tower",
            address = "1 St",
            city = "Lisbon",
            country = "PT",
            postalCode = "1000-001",
            buildingType = "Corporate",
            estimatedDailyAudience = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WriteEndpoint_WithOperatorToken_CreatesBuilding()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(TestTokenIssuer.IssueOperatorToken());

        var response = await client.PostAsJsonAsync("/api/buildings", new
        {
            name = "Tower One",
            address = "123 Main St",
            city = "Lisbon",
            country = "Portugal",
            postalCode = "1000-001",
            buildingType = "Corporate",
            estimatedDailyAudience = 500
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreativeApprove_WithOperatorToken_Returns403()
    {
        await _factory.ResetDatabaseAsync();
        var factory = (TestWebApplicationFactory)_factory;
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueOperatorToken());

        var advertiser = await client.PostAsJsonAsync("/api/advertisers", new
        {
            name = "Acme",
            legalName = "Acme Ltd",
            taxId = "PT123",
            contactName = "Jane",
            contactEmail = "jane@acme.test",
            phone = "+351",
            status = "Active"
        });
        advertiser.EnsureSuccessStatusCode();
        var advertiserBody = await advertiser.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(advertiserBody);

        var creative = await client.PostAsJsonAsync("/api/creatives", new
        {
            advertiserId = advertiserBody!.Id,
            name = "Lobby Promo",
            mediaUrl = "https://cdn.example.com/creative.jpg",
            mediaType = "Image",
            durationSeconds = 10
        });
        creative.EnsureSuccessStatusCode();
        var creativeBody = await creative.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(creativeBody);

        var submit = await client.PostAsync($"/api/creatives/{creativeBody!.Id}/submit-for-review", null);
        submit.EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/creatives/{creativeBody.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreativeApprove_WithAdminToken_Succeeds()
    {
        await _factory.ResetDatabaseAsync();
        var factory = (TestWebApplicationFactory)_factory;
        var client = factory.CreateAuthenticatedClient(TestTokenIssuer.IssueAdminToken());

        var advertiser = await client.PostAsJsonAsync("/api/advertisers", new
        {
            name = "Acme",
            legalName = "Acme Ltd",
            taxId = "PT123",
            contactName = "Jane",
            contactEmail = "jane@acme.test",
            phone = "+351",
            status = "Active"
        });
        advertiser.EnsureSuccessStatusCode();
        var advertiserBody = await advertiser.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(advertiserBody);

        var creative = await client.PostAsJsonAsync("/api/creatives", new
        {
            advertiserId = advertiserBody!.Id,
            name = "Lobby Promo",
            mediaUrl = "https://cdn.example.com/creative.jpg",
            mediaType = "Image",
            durationSeconds = 10
        });
        creative.EnsureSuccessStatusCode();
        var creativeBody = await creative.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(creativeBody);

        var submit = await client.PostAsync($"/api/creatives/{creativeBody!.Id}/submit-for-review", null);
        submit.EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/creatives/{creativeBody.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadEndpoint_WithoutToken_Succeeds()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/buildings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SeedUserAsync(string username, string password, Domain.Enums.UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElevatorAds.Infrastructure.Persistence.AppDbContext>();
        var existing = await context.Users.FirstOrDefaultAsync(item => item.Username == username);
        if (existing is not null)
        {
            return;
        }

        context.Users.Add(new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private sealed record IdResponse(Guid Id);
}
