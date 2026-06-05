using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public sealed class AdvertiserApiKey
{
    public Guid Id { get; set; }
    public Guid AdvertiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Advertiser? Advertiser { get; set; }
}
