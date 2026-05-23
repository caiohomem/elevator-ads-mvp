using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IBuildingRepository
{
    Task<IEnumerable<Building>> GetAllAsync();
    Task<Building?> GetByIdAsync(Guid id);
    Task<Building> AddAsync(Building building);
    Task<Building?> UpdateAsync(Building building);
    Task<bool> DeleteAsync(Guid id);
}
