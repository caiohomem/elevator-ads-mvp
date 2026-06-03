using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfAdvertiserRepository : IAdvertiserRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfAdvertiserRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Advertiser>> GetAllAsync() =>
        await _context.Advertisers.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<Advertiser?> GetByIdAsync(Guid id) =>
        await _context.Advertisers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Advertiser> AddAsync(Advertiser advertiser)
    {
        await _context.Advertisers.AddAsync(advertiser);
        await _context.SaveChangesAsync();
        return advertiser;
    }

    public async Task<Advertiser?> UpdateAsync(Advertiser advertiser)
    {
        var existing = await _context.Advertisers.FirstOrDefaultAsync(item => item.Id == advertiser.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(advertiser);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Advertisers.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Advertisers.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
