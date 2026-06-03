using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignRepository : ICampaignRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Campaign>> GetAllAsync() =>
        await _context.Campaigns.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<Campaign?> GetByIdAsync(Guid id) =>
        await _context.Campaigns.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Campaign> AddAsync(Campaign campaign)
    {
        await _context.Campaigns.AddAsync(campaign);
        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task<Campaign?> UpdateAsync(Campaign campaign)
    {
        var existing = await _context.Campaigns.FirstOrDefaultAsync(item => item.Id == campaign.Id);
        if (existing is null)
        {
            return null;
        }

        campaign.UpdatedAt = DateTime.UtcNow;
        _context.Entry(existing).CurrentValues.SetValues(campaign);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Campaigns.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Campaigns.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
