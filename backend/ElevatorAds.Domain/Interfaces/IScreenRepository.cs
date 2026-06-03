using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface IScreenRepository
{
    Task<IEnumerable<Screen>> GetAllAsync();
    Task<(IEnumerable<Screen> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Screen?> GetByIdAsync(Guid id);
    Task<Screen> AddAsync(Screen screen);
    Task<Screen?> UpdateAsync(Screen screen);
    Task<bool> DeleteAsync(Guid id);
    Task<Screen?> UpdateLastSeenAtAsync(Guid id, DateTime lastSeenAt);
}
