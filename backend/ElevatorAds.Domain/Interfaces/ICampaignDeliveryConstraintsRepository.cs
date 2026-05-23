using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignDeliveryConstraintsRepository
{
    Task<CampaignDeliveryConstraints?> GetByCampaignIdAsync(Guid campaignId);
    Task<CampaignDeliveryConstraints> UpsertAsync(CampaignDeliveryConstraints constraints);
}
