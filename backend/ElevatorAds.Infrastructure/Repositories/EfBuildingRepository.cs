using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfBuildingRepository : IBuildingRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfBuildingRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Building>> GetAllAsync() =>
        await _context.Buildings.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<Building?> GetByIdAsync(Guid id) =>
        await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Building> AddAsync(Building building)
    {
        await _context.Buildings.AddAsync(building);
        await _context.SaveChangesAsync();
        return building;
    }

    public async Task<Building?> UpdateAsync(Building building)
    {
        var existing = await _context.Buildings.FirstOrDefaultAsync(item => item.Id == building.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(building);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Buildings.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Buildings.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
