using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryAdvertiserRepository : IAdvertiserRepository
{
    private readonly Dictionary<Guid, Advertiser> _advertisers = new();

    public Task<IEnumerable<Advertiser>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Advertiser>>(_advertisers.Values.OrderBy(advertiser => advertiser.CreatedAt).ToList());

    public Task<Advertiser?> GetByIdAsync(Guid id)
    {
        _advertisers.TryGetValue(id, out var advertiser);
        return Task.FromResult(advertiser);
    }

    public Task<Advertiser> AddAsync(Advertiser advertiser)
    {
        _advertisers[advertiser.Id] = advertiser;
        return Task.FromResult(advertiser);
    }

    public Task<Advertiser?> UpdateAsync(Advertiser advertiser)
    {
        if (!_advertisers.ContainsKey(advertiser.Id))
        {
            return Task.FromResult<Advertiser?>(null);
        }

        _advertisers[advertiser.Id] = advertiser;
        return Task.FromResult<Advertiser?>(advertiser);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_advertisers.Remove(id));
}
