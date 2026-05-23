using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignCreativeRepository
{
    Task<IEnumerable<CampaignCreative>> GetByCampaignIdAsync(Guid campaignId);
    Task<CampaignCreative?> GetAsync(Guid campaignId, Guid creativeId);
    Task<CampaignCreative> AddAsync(CampaignCreative assignment);
    Task<bool> DeleteAsync(Guid campaignId, Guid creativeId);
}
