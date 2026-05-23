using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryCampaignDeliveryConstraintsRepository : ICampaignDeliveryConstraintsRepository
{
    private readonly Dictionary<Guid, CampaignDeliveryConstraints> _constraintsByCampaignId = new();

    public Task<CampaignDeliveryConstraints?> GetByCampaignIdAsync(Guid campaignId)
    {
        _constraintsByCampaignId.TryGetValue(campaignId, out var constraints);
        return Task.FromResult(constraints);
    }

    public Task<CampaignDeliveryConstraints> UpsertAsync(CampaignDeliveryConstraints constraints)
    {
        var now = DateTime.UtcNow;

        if (_constraintsByCampaignId.TryGetValue(constraints.CampaignId, out var existing))
        {
            constraints.Id = existing.Id;
            constraints.CreatedAt = existing.CreatedAt;
            constraints.UpdatedAt = now;
        }
        else
        {
            constraints.CreatedAt = now;
            constraints.UpdatedAt = now;
        }

        _constraintsByCampaignId[constraints.CampaignId] = constraints;
        return Task.FromResult(constraints);
    }
}
