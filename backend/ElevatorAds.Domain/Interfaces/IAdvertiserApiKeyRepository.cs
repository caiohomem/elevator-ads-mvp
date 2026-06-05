using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IAdvertiserApiKeyRepository
{
    Task<IReadOnlyList<AdvertiserApiKey>> GetByAdvertiserIdAsync(Guid advertiserId);
    Task<AdvertiserApiKey?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<AdvertiserApiKey>> GetByKeyPrefixAsync(string keyPrefix);
    Task<AdvertiserApiKey> AddAsync(AdvertiserApiKey apiKey);
    Task<AdvertiserApiKey?> UpdateAsync(AdvertiserApiKey apiKey);
}
