using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface IBuildingRepository
{
    Task<IEnumerable<Building>> GetAllAsync();
    Task<(IEnumerable<Building> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Building?> GetByIdAsync(Guid id);
    Task<Building> AddAsync(Building building);
    Task<Building?> UpdateAsync(Building building);
    Task<bool> DeleteAsync(Guid id);
}
