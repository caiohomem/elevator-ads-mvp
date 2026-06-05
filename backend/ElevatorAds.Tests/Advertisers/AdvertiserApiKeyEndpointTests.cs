using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElevatorAds.Infrastructure.Persistence;
using ElevatorAds.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Tests.Advertisers;

public sealed class AdvertiserApiKeyEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AdvertiserApiKeyEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostApiKey_CreatesAdvertiserApiKey()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);

        var response = await client.PostAsJsonAsync($"/api/advertisers/{advertiser.Id}/api-keys", new CreateAdvertiserApiKeyRequest(
            "Partner test key",
            ["forecast:create"],
            DateTime.UtcNow.AddDays(30)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreateAdvertiserApiKeyResponse>();
        Assert.NotNull(created);
        Assert.Equal("Partner test key", created!.Name);
        Assert.Equal(advertiser.Id, created.AdvertiserId);
        Assert.Equal("Active", created.Status);
        Assert.StartsWith("elev_test_", created.KeyPrefix);
        Assert.StartsWith(created.KeyPrefix, created.PlainApiKey);
    }

    [Fact]
    public async Task PlainApiKey_IsReturnedOnlyAtCreationTime()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/advertisers/{advertiser.Id}/api-keys", new CreateAdvertiserApiKeyRequest(
            "One time key",
            ["forecast:create"],
            null));
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync($"/api/advertisers/{advertiser.Id}/api-keys");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadAsStringAsync();

        Assert.Contains("plainApiKey", await createResponse.Content.ReadAsStringAsync());
        Assert.DoesNotContain("plainApiKey", listJson);
    }

    [Fact]
    public async Task StoredValue_IsHashed_NotPlainText()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);

        var response = await client.PostAsJsonAsync($"/api/advertisers/{advertiser.Id}/api-keys", new CreateAdvertiserApiKeyRequest(
            "Hashed key",
            ["reports:read"],
            null));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreateAdvertiserApiKeyResponse>();
        Assert.NotNull(created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = db.AdvertiserApiKeys.Single();

        Assert.Equal(created!.KeyPrefix, stored.KeyPrefix);
        Assert.NotEqual(created.PlainApiKey, stored.KeyHash);
        Assert.Equal(64, stored.KeyHash.Length);
        Assert.DoesNotContain(created.PlainApiKey, stored.KeyHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetApiKeys_ListOnlyShowsPrefixAndMetadata()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var createResponse = await client.PostAsJsonAsync($"/api/advertisers/{advertiser.Id}/api-keys", new CreateAdvertiserApiKeyRequest(
            "List key",
            ["inventory:read", "reports:read"],
            null));
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/advertisers/{advertiser.Id}/api-keys");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<List<AdvertiserApiKeyDto>>(json, JsonOptions);

        Assert.NotNull(keys);
        Assert.Single(keys!);
        Assert.Equal("List key", keys[0].Name);
        Assert.NotEmpty(keys[0].KeyPrefix);
        Assert.DoesNotContain("keyHash", json);
        Assert.DoesNotContain("plainApiKey", json);
    }

    [Fact]
    public async Task PostRevoke_RevokesApiKey()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateApiKeyAsync(client, advertiser.Id, ["reports:read"]);

        var response = await client.PostAsync($"/api/advertisers/{advertiser.Id}/api-keys/{created.Id}/revoke", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var revoked = await response.Content.ReadFromJsonAsync<AdvertiserApiKeyDto>();
        Assert.NotNull(revoked);
        Assert.Equal("Revoked", revoked!.Status);
        Assert.NotNull(revoked.RevokedAt);
    }

    [Fact]
    public async Task Validation_MissingApiKey_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/programmatic/internal/api-key-check/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validation_InvalidApiKey_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "elev_test_deadbeef_invalid");

        var response = await client.GetAsync("/api/programmatic/internal/api-key-check/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validation_MissingScope_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = CreateClient();
        var advertiser = await CreateAdvertiserAsync(adminClient);
        var created = await CreateApiKeyAsync(adminClient, advertiser.Id, ["forecast:create"]);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", created.PlainApiKey);

        var response = await client.GetAsync("/api/programmatic/internal/api-key-check/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validation_ValidApiKeyAndScope_IsAccepted()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = CreateClient();
        var advertiser = await CreateAdvertiserAsync(adminClient);
        var created = await CreateApiKeyAsync(adminClient, advertiser.Id, ["reports:read"]);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", created.PlainApiKey);

        var response = await client.GetAsync("/api/programmatic/internal/api-key-check/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiKeyCheckResponse>();
        Assert.NotNull(payload);
        Assert.Equal(advertiser.Id, payload!.AdvertiserId);
        Assert.Equal(created.KeyPrefix, payload.KeyPrefix);
    }

    [Fact]
    public async Task Validation_Success_UpdatesLastUsedAt()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = CreateClient();
        var advertiser = await CreateAdvertiserAsync(adminClient);
        var created = await CreateApiKeyAsync(adminClient, advertiser.Id, ["reports:read"]);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", created.PlainApiKey);
        var response = await client.GetAsync("/api/programmatic/internal/api-key-check/reports");
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = db.AdvertiserApiKeys.Single(item => item.Id == created.Id);

        Assert.NotNull(stored.LastUsedAt);
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/advertisers", new CreateAdvertiserRequest(
            $"Advertiser-{Guid.NewGuid():N}",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active"));
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private async Task<CreateAdvertiserApiKeyResponse> CreateApiKeyAsync(HttpClient client, Guid advertiserId, List<string> scopes)
    {
        var response = await client.PostAsJsonAsync($"/api/advertisers/{advertiserId}/api-keys", new CreateAdvertiserApiKeyRequest(
            "Partner key",
            scopes,
            DateTime.UtcNow.AddDays(14)));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreateAdvertiserApiKeyResponse>();
        Assert.NotNull(created);
        return created!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record CreateAdvertiserRequest(
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status);

    private sealed record AdvertiserDto(Guid Id);

    private sealed record CreateAdvertiserApiKeyRequest(
        string Name,
        List<string> Scopes,
        DateTime? ExpiresAt);

    private sealed record AdvertiserApiKeyDto(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        string KeyPrefix,
        List<string> Scopes,
        string Status,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastUsedAt,
        DateTime? RevokedAt);

    private sealed record CreateAdvertiserApiKeyResponse(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        string KeyPrefix,
        string PlainApiKey,
        List<string> Scopes,
        string Status,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastUsedAt,
        DateTime? RevokedAt);

    private sealed record ApiKeyCheckResponse(Guid AdvertiserId, Guid ApiKeyId, string KeyPrefix, string RequiredScope);
}
