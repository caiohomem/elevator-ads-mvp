using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryBuildingRepository : IBuildingRepository
{
    private readonly Dictionary<Guid, Building> _buildings = new();

    public Task<IEnumerable<Building>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Building>>(_buildings.Values.OrderBy(building => building.CreatedAt).ToList());

    public Task<Building?> GetByIdAsync(Guid id)
    {
        _buildings.TryGetValue(id, out var building);
        return Task.FromResult(building);
    }

    public Task<Building> AddAsync(Building building)
    {
        _buildings[building.Id] = building;
        return Task.FromResult(building);
    }

    public Task<Building?> UpdateAsync(Building building)
    {
        if (!_buildings.ContainsKey(building.Id))
        {
            return Task.FromResult<Building?>(null);
        }

        _buildings[building.Id] = building;
        return Task.FromResult<Building?>(building);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_buildings.Remove(id));
}
