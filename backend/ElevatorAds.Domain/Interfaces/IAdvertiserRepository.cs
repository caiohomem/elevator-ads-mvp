using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface IAdvertiserRepository
{
    Task<IEnumerable<Advertiser>> GetAllAsync();
    Task<(IEnumerable<Advertiser> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Advertiser?> GetByIdAsync(Guid id);
    Task<Advertiser> AddAsync(Advertiser advertiser);
    Task<Advertiser?> UpdateAsync(Advertiser advertiser);
    Task<bool> DeleteAsync(Guid id);
}
