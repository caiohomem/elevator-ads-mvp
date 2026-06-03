using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfScreenRepository : IScreenRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfScreenRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Screen>> GetAllAsync() =>
        await _context.Screens.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<Screen?> GetByIdAsync(Guid id) =>
        await _context.Screens.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Screen> AddAsync(Screen screen)
    {
        await _context.Screens.AddAsync(screen);
        await _context.SaveChangesAsync();
        return screen;
    }

    public async Task<Screen?> UpdateAsync(Screen screen)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == screen.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(screen);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Screens.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Screen?> UpdateLastSeenAtAsync(Guid id, DateTime lastSeenAt)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.LastSeenAt = lastSeenAt;
        existing.UpdatedAt = lastSeenAt;
        await _context.SaveChangesAsync();
        return existing;
    }
}
