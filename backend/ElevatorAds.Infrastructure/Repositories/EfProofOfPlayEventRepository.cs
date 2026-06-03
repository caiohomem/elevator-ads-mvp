using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfProofOfPlayEventRepository : IProofOfPlayEventRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfProofOfPlayEventRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<ProofOfPlayEvent>> GetAllAsync() =>
        await _context.ProofOfPlayEvents
            .AsNoTracking()
            .OrderByDescending(item => item.PlayedAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

    public async Task<ProofOfPlayEvent> AddAsync(ProofOfPlayEvent proofOfPlay)
    {
        await _context.ProofOfPlayEvents.AddAsync(proofOfPlay);
        await _context.SaveChangesAsync();
        return proofOfPlay;
    }

    public async Task<IEnumerable<ProofOfPlayEvent>> GetByScreenIdAsync(Guid screenId) =>
        await _context.ProofOfPlayEvents
            .AsNoTracking()
            .Where(item => item.ScreenId == screenId)
            .OrderByDescending(item => item.PlayedAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<ProofOfPlayEvent>> GetByCampaignIdAsync(Guid campaignId) =>
        await _context.ProofOfPlayEvents
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderByDescending(item => item.PlayedAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<ProofOfPlayEvent>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await _context.ProofOfPlayEvents
            .AsNoTracking()
            .Where(item => item.PlayedAt >= from && item.PlayedAt < to)
            .OrderByDescending(item => item.PlayedAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();
}
