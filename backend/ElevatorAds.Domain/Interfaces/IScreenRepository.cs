using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IScreenRepository
{
    Task<IEnumerable<Screen>> GetAllAsync();
    Task<Screen?> GetByIdAsync(Guid id);
    Task<Screen> AddAsync(Screen screen);
    Task<Screen?> UpdateAsync(Screen screen);
    Task<bool> DeleteAsync(Guid id);
    Task<Screen?> UpdateLastSeenAtAsync(Guid id, DateTime lastSeenAt);
}
