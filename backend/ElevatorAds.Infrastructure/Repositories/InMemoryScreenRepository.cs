using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryScreenRepository : IScreenRepository
{
    private readonly Dictionary<Guid, Screen> _screens = new();

    public Task<IEnumerable<Screen>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Screen>>(_screens.Values.OrderBy(screen => screen.CreatedAt).ToList());

    public Task<Screen?> GetByIdAsync(Guid id)
    {
        _screens.TryGetValue(id, out var screen);
        return Task.FromResult(screen);
    }

    public Task<Screen> AddAsync(Screen screen)
    {
        _screens[screen.Id] = screen;
        return Task.FromResult(screen);
    }

    public Task<Screen?> UpdateAsync(Screen screen)
    {
        if (!_screens.ContainsKey(screen.Id))
        {
            return Task.FromResult<Screen?>(null);
        }

        _screens[screen.Id] = screen;
        return Task.FromResult<Screen?>(screen);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_screens.Remove(id));

    public Task<Screen?> UpdateLastSeenAtAsync(Guid id, DateTime lastSeenAt)
    {
        if (!_screens.TryGetValue(id, out var screen))
        {
            return Task.FromResult<Screen?>(null);
        }

        screen.LastSeenAt = lastSeenAt;
        screen.UpdatedAt = lastSeenAt;
        return Task.FromResult<Screen?>(screen);
    }
}
