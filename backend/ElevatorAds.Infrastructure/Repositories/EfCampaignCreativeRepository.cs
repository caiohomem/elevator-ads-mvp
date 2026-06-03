using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignCreativeRepository : ICampaignCreativeRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignCreativeRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<CampaignCreative>> GetByCampaignIdAsync(Guid campaignId) =>
        await _context.CampaignCreatives
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();

    public async Task<CampaignCreative?> GetAsync(Guid campaignId, Guid creativeId) =>
        await _context.CampaignCreatives
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CampaignId == campaignId && item.CreativeId == creativeId);

    public async Task<CampaignCreative> AddAsync(CampaignCreative assignment)
    {
        await _context.CampaignCreatives.AddAsync(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> DeleteAsync(Guid campaignId, Guid creativeId)
    {
        var existing = await _context.CampaignCreatives
            .FirstOrDefaultAsync(item => item.CampaignId == campaignId && item.CreativeId == creativeId);
        if (existing is null)
        {
            return false;
        }

        _context.CampaignCreatives.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
