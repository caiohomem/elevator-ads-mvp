namespace ElevatorAds.Application.Advertisers.Dtos;

public sealed record CreateAdvertiserApiKeyRequest(
    string Name,
    List<string> Scopes,
    DateTime? ExpiresAt);

public sealed record AdvertiserApiKeyDto(
    Guid Id,
    Guid AdvertiserId,
    string Name,
    string KeyPrefix,
    IReadOnlyList<string> Scopes,
    string Status,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime? RevokedAt);

public sealed record CreateAdvertiserApiKeyResponse(
    Guid Id,
    Guid AdvertiserId,
    string Name,
    string KeyPrefix,
    string PlainApiKey,
    IReadOnlyList<string> Scopes,
    string Status,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime? RevokedAt);

public sealed record ValidateAdvertiserApiKeyResult(
    bool IsValid,
    string? Error,
    Guid? AdvertiserId,
    Guid? ApiKeyId,
    string? KeyPrefix,
    IReadOnlyList<string>? Scopes)
{
    public static ValidateAdvertiserApiKeyResult Success(Guid advertiserId, Guid apiKeyId, string keyPrefix, IReadOnlyList<string> scopes) =>
        new(true, null, advertiserId, apiKeyId, keyPrefix, scopes);

    public static ValidateAdvertiserApiKeyResult Failure(string error) =>
        new(false, error, null, null, null, null);
}
