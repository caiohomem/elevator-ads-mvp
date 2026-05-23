using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignRepository
{
    Task<IEnumerable<Campaign>> GetAllAsync();
    Task<Campaign?> GetByIdAsync(Guid id);
    Task<Campaign> AddAsync(Campaign campaign);
    Task<Campaign?> UpdateAsync(Campaign campaign);
    Task<bool> DeleteAsync(Guid id);
}
