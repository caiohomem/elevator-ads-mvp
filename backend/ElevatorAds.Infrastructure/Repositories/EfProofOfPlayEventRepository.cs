using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
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

    public async Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedAsync(PagedQuery query) =>
        await GetPagedAsyncInternal(_context.ProofOfPlayEvents.AsNoTracking(), query);

    public async Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedByScreenIdAsync(Guid screenId, PagedQuery query) =>
        await GetPagedAsyncInternal(_context.ProofOfPlayEvents.AsNoTracking().Where(item => item.ScreenId == screenId), query);

    public async Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedByCampaignIdAsync(Guid campaignId, PagedQuery query) =>
        await GetPagedAsyncInternal(_context.ProofOfPlayEvents.AsNoTracking().Where(item => item.CampaignId == campaignId), query);

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

    private static async Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedAsyncInternal(
        IQueryable<ProofOfPlayEvent> query,
        PagedQuery pagedQuery)
    {
        var sortedQuery = ApplySort(query, pagedQuery);
        var totalCount = await sortedQuery.CountAsync();
        var items = await sortedQuery
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    private static IOrderedQueryable<ProofOfPlayEvent> ApplySort(IQueryable<ProofOfPlayEvent> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "playedat" when descending => query.OrderByDescending(item => item.PlayedAt).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "playedat" => query.OrderBy(item => item.PlayedAt).ThenBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.PlayedAt).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
