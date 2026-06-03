using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignRepository
{
    Task<IEnumerable<Campaign>> GetAllAsync();
    Task<(IEnumerable<Campaign> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Campaign?> GetByIdAsync(Guid id);
    Task<Campaign> AddAsync(Campaign campaign);
    Task<Campaign?> UpdateAsync(Campaign campaign);
    Task<bool> DeleteAsync(Guid id);
}
