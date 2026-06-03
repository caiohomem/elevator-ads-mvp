using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCreativeRepository : ICreativeRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCreativeRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Creative>> GetAllAsync() =>
        await _context.Creatives.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<Creative?> GetByIdAsync(Guid id) =>
        await _context.Creatives.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Creative> AddAsync(Creative creative)
    {
        await _context.Creatives.AddAsync(creative);
        await _context.SaveChangesAsync();
        return creative;
    }

    public async Task<Creative?> UpdateAsync(Creative creative)
    {
        var existing = await _context.Creatives.FirstOrDefaultAsync(item => item.Id == creative.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(creative);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Creatives.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Creatives.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
