using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignDeliveryConstraintsRepository : ICampaignDeliveryConstraintsRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignDeliveryConstraintsRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<CampaignDeliveryConstraints?> GetByCampaignIdAsync(Guid campaignId) =>
        await _context.CampaignDeliveryConstraints
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CampaignId == campaignId);

    public async Task<CampaignDeliveryConstraints> UpsertAsync(CampaignDeliveryConstraints constraints)
    {
        var existing = await _context.CampaignDeliveryConstraints
            .FirstOrDefaultAsync(item => item.CampaignId == constraints.CampaignId);

        var now = DateTime.UtcNow;

        if (existing is null)
        {
            constraints.Id = constraints.Id == Guid.Empty ? Guid.NewGuid() : constraints.Id;
            constraints.CreatedAt = now;
            constraints.UpdatedAt = now;
            await _context.CampaignDeliveryConstraints.AddAsync(constraints);
        }
        else
        {
            constraints.Id = existing.Id;
            constraints.CreatedAt = existing.CreatedAt;
            constraints.UpdatedAt = now;
            _context.Entry(existing).CurrentValues.SetValues(constraints);
        }

        await _context.SaveChangesAsync();
        return constraints;
    }
}
