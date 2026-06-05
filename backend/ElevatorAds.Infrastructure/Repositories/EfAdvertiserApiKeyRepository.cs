using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfAdvertiserApiKeyRepository : IAdvertiserApiKeyRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfAdvertiserApiKeyRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<AdvertiserApiKey>> GetByAdvertiserIdAsync(Guid advertiserId) =>
        await _context.AdvertiserApiKeys
            .AsNoTracking()
            .Where(item => item.AdvertiserId == advertiserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync();

    public async Task<AdvertiserApiKey?> GetByIdAsync(Guid id) =>
        await _context.AdvertiserApiKeys.FirstOrDefaultAsync(item => item.Id == id);

    public async Task<IReadOnlyList<AdvertiserApiKey>> GetByKeyPrefixAsync(string keyPrefix) =>
        await _context.AdvertiserApiKeys
            .Where(item => item.KeyPrefix == keyPrefix)
            .ToListAsync();

    public async Task<AdvertiserApiKey> AddAsync(AdvertiserApiKey apiKey)
    {
        await _context.AdvertiserApiKeys.AddAsync(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task<AdvertiserApiKey?> UpdateAsync(AdvertiserApiKey apiKey)
    {
        var existing = await _context.AdvertiserApiKeys.FirstOrDefaultAsync(item => item.Id == apiKey.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(apiKey);
        existing.Scopes = apiKey.Scopes.ToList();
        await _context.SaveChangesAsync();
        return existing;
    }
}
