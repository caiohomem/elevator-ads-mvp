using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryCampaignRepository : ICampaignRepository
{
    private readonly Dictionary<Guid, Campaign> _campaigns = new();

    public Task<IEnumerable<Campaign>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Campaign>>(_campaigns.Values.OrderBy(campaign => campaign.CreatedAt).ToList());

    public Task<Campaign?> GetByIdAsync(Guid id)
    {
        _campaigns.TryGetValue(id, out var campaign);
        return Task.FromResult(campaign);
    }

    public Task<Campaign> AddAsync(Campaign campaign)
    {
        _campaigns[campaign.Id] = campaign;
        return Task.FromResult(campaign);
    }

    public Task<Campaign?> UpdateAsync(Campaign campaign)
    {
        if (!_campaigns.ContainsKey(campaign.Id))
        {
            return Task.FromResult<Campaign?>(null);
        }

        campaign.UpdatedAt = DateTime.UtcNow;
        _campaigns[campaign.Id] = campaign;
        return Task.FromResult<Campaign?>(campaign);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_campaigns.Remove(id));
}
