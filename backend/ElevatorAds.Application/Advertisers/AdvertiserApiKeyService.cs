using System.Security.Cryptography;
using System.Text;
using ElevatorAds.Application.Advertisers.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Advertisers;

public sealed class AdvertiserApiKeyService
{
    public const string InventoryReadScope = "inventory:read";
    public const string ForecastCreateScope = "forecast:create";
    public const string BookingCreateScope = "booking:create";
    public const string ReportsReadScope = "reports:read";

    private static readonly string[] AllowedScopes =
    [
        InventoryReadScope,
        ForecastCreateScope,
        BookingCreateScope,
        ReportsReadScope
    ];

    private readonly IAdvertiserApiKeyRepository _apiKeyRepository;
    private readonly IAdvertiserRepository _advertiserRepository;
    private readonly TimeProvider _timeProvider;
    private readonly string _environmentPrefix;

    public AdvertiserApiKeyService(
        IAdvertiserApiKeyRepository apiKeyRepository,
        IAdvertiserRepository advertiserRepository,
        TimeProvider timeProvider,
        string environmentPrefix)
    {
        _apiKeyRepository = apiKeyRepository;
        _advertiserRepository = advertiserRepository;
        _timeProvider = timeProvider;
        _environmentPrefix = environmentPrefix;
    }

    public async Task<ServiceResult<IReadOnlyList<AdvertiserApiKeyDto>?>> GetByAdvertiserIdAsync(Guid advertiserId)
    {
        if (await _advertiserRepository.GetByIdAsync(advertiserId) is null)
        {
            return ServiceResult<IReadOnlyList<AdvertiserApiKeyDto>?>.Success(null);
        }

        var apiKeys = await _apiKeyRepository.GetByAdvertiserIdAsync(advertiserId);
        return ServiceResult<IReadOnlyList<AdvertiserApiKeyDto>?>.Success(apiKeys.Select(Map).ToList());
    }

    public async Task<ServiceResult<CreateAdvertiserApiKeyResponse?>> CreateAsync(
        Guid advertiserId,
        CreateAdvertiserApiKeyRequest request)
    {
        if (await _advertiserRepository.GetByIdAsync(advertiserId) is null)
        {
            return ServiceResult<CreateAdvertiserApiKeyResponse?>.Success(null);
        }

        if (Validate(request) is { } validationError)
        {
            return ServiceResult<CreateAdvertiserApiKeyResponse?>.Failure(validationError);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var publicId = CreatePublicId();
        var keyPrefix = $"{_environmentPrefix}{publicId}";
        var secret = CreateSecret();
        var plainApiKey = $"{keyPrefix}_{secret}";
        var keyHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainApiKey));

        var apiKey = new AdvertiserApiKey
        {
            Id = Guid.NewGuid(),
            AdvertiserId = advertiserId,
            Name = request.Name.Trim(),
            KeyPrefix = keyPrefix,
            KeyHash = Convert.ToHexString(keyHashBytes),
            Scopes = request.Scopes.Select(scope => scope.Trim()).Distinct(StringComparer.Ordinal).ToList(),
            Status = ApiKeyStatus.Active,
            CreatedAt = now,
            ExpiresAt = request.ExpiresAt?.ToUniversalTime()
        };

        var created = await _apiKeyRepository.AddAsync(apiKey);
        return ServiceResult<CreateAdvertiserApiKeyResponse?>.Success(MapCreated(created, plainApiKey));
    }

    public async Task<ServiceResult<AdvertiserApiKeyDto?>> RevokeAsync(Guid advertiserId, Guid apiKeyId)
    {
        if (await _advertiserRepository.GetByIdAsync(advertiserId) is null)
        {
            return ServiceResult<AdvertiserApiKeyDto?>.Success(null);
        }

        var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId);
        if (apiKey is null || apiKey.AdvertiserId != advertiserId)
        {
            return ServiceResult<AdvertiserApiKeyDto?>.Success(null);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        apiKey.Status = ApiKeyStatus.Revoked;
        apiKey.RevokedAt = now;

        var updated = await _apiKeyRepository.UpdateAsync(apiKey);
        return ServiceResult<AdvertiserApiKeyDto?>.Success(updated is null ? null : Map(updated));
    }

    public async Task<ValidateAdvertiserApiKeyResult> ValidateAsync(string? rawKey, string requiredScope)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return ValidateAdvertiserApiKeyResult.Failure("API key is required.");
        }

        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return ValidateAdvertiserApiKeyResult.Failure("Required scope is missing.");
        }

        var normalizedKey = rawKey.Trim();
        var keyPrefix = ExtractKeyPrefix(normalizedKey);
        if (keyPrefix is null)
        {
            return ValidateAdvertiserApiKeyResult.Failure("Invalid API key.");
        }

        var apiKeys = await _apiKeyRepository.GetByKeyPrefixAsync(keyPrefix);
        if (apiKeys.Count == 0)
        {
            return ValidateAdvertiserApiKeyResult.Failure("Invalid API key.");
        }

        var providedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedKey));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var apiKey in apiKeys)
        {
            if (!TryMatchHash(apiKey.KeyHash, providedHashBytes))
            {
                continue;
            }

            var effectiveStatus = GetEffectiveStatus(apiKey, now);
            if (effectiveStatus == ApiKeyStatus.Expired && apiKey.Status == ApiKeyStatus.Active)
            {
                apiKey.Status = ApiKeyStatus.Expired;
                await _apiKeyRepository.UpdateAsync(apiKey);
            }

            if (effectiveStatus != ApiKeyStatus.Active)
            {
                return ValidateAdvertiserApiKeyResult.Failure("API key is not active.");
            }

            if (!apiKey.Scopes.Contains(requiredScope, StringComparer.Ordinal))
            {
                return ValidateAdvertiserApiKeyResult.Failure("API key does not include the required scope.");
            }

            apiKey.LastUsedAt = now;
            await _apiKeyRepository.UpdateAsync(apiKey);
            return ValidateAdvertiserApiKeyResult.Success(apiKey.AdvertiserId, apiKey.Id, apiKey.KeyPrefix, apiKey.Scopes);
        }

        return ValidateAdvertiserApiKeyResult.Failure("Invalid API key.");
    }

    private static string? Validate(CreateAdvertiserApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (request.Scopes.Count == 0)
        {
            return "At least one scope is required.";
        }

        foreach (var scope in request.Scopes)
        {
            if (!AllowedScopes.Contains(scope.Trim(), StringComparer.Ordinal))
            {
                return $"Invalid scope: {scope}.";
            }
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value.ToUniversalTime() <= DateTime.UtcNow)
        {
            return "ExpiresAt must be in the future.";
        }

        return null;
    }

    private static string CreatePublicId()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateSecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? ExtractKeyPrefix(string rawKey)
    {
        if (rawKey.StartsWith("elev_test_", StringComparison.Ordinal) ||
            rawKey.StartsWith("elev_live_", StringComparison.Ordinal))
        {
            return rawKey.Length >= 19 ? rawKey[..18] : null;
        }

        return null;
    }

    private static bool TryMatchHash(string storedHexHash, byte[] providedHashBytes)
    {
        try
        {
            var storedHashBytes = Convert.FromHexString(storedHexHash);
            return storedHashBytes.Length == providedHashBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(storedHashBytes, providedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ApiKeyStatus GetEffectiveStatus(AdvertiserApiKey apiKey, DateTime now)
    {
        if (apiKey.Status == ApiKeyStatus.Revoked || apiKey.RevokedAt.HasValue)
        {
            return ApiKeyStatus.Revoked;
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= now)
        {
            return ApiKeyStatus.Expired;
        }

        return apiKey.Status;
    }

    private static AdvertiserApiKeyDto Map(AdvertiserApiKey apiKey) =>
        new(
            apiKey.Id,
            apiKey.AdvertiserId,
            apiKey.Name,
            apiKey.KeyPrefix,
            apiKey.Scopes,
            GetEffectiveStatus(apiKey, DateTime.UtcNow).ToString(),
            apiKey.CreatedAt,
            apiKey.ExpiresAt,
            apiKey.LastUsedAt,
            apiKey.RevokedAt);

    private static CreateAdvertiserApiKeyResponse MapCreated(AdvertiserApiKey apiKey, string plainApiKey) =>
        new(
            apiKey.Id,
            apiKey.AdvertiserId,
            apiKey.Name,
            apiKey.KeyPrefix,
            plainApiKey,
            apiKey.Scopes,
            apiKey.Status.ToString(),
            apiKey.CreatedAt,
            apiKey.ExpiresAt,
            apiKey.LastUsedAt,
            apiKey.RevokedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);
        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
