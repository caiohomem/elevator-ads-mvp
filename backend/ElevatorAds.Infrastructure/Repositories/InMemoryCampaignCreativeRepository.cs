using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryCampaignCreativeRepository : ICampaignCreativeRepository
{
    private readonly Dictionary<Guid, CampaignCreative> _assignments = new();

    public Task<IEnumerable<CampaignCreative>> GetByCampaignIdAsync(Guid campaignId) =>
        Task.FromResult<IEnumerable<CampaignCreative>>(
            _assignments.Values
                .Where(assignment => assignment.CampaignId == campaignId)
                .OrderBy(assignment => assignment.CreatedAt)
                .ToList());

    public Task<CampaignCreative?> GetAsync(Guid campaignId, Guid creativeId)
    {
        var assignment = _assignments.Values.FirstOrDefault(item =>
            item.CampaignId == campaignId && item.CreativeId == creativeId);

        return Task.FromResult(assignment);
    }

    public Task<CampaignCreative> AddAsync(CampaignCreative assignment)
    {
        _assignments[assignment.Id] = assignment;
        return Task.FromResult(assignment);
    }

    public async Task<bool> DeleteAsync(Guid campaignId, Guid creativeId)
    {
        var assignment = await GetAsync(campaignId, creativeId);
        return assignment is not null && _assignments.Remove(assignment.Id);
    }
}
