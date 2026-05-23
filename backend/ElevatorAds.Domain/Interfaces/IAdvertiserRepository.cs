using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IAdvertiserRepository
{
    Task<IEnumerable<Advertiser>> GetAllAsync();
    Task<Advertiser?> GetByIdAsync(Guid id);
    Task<Advertiser> AddAsync(Advertiser advertiser);
    Task<Advertiser?> UpdateAsync(Advertiser advertiser);
    Task<bool> DeleteAsync(Guid id);
}
