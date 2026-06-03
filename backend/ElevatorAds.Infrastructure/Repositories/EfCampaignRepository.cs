using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignRepository : ICampaignRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Campaign>> GetAllAsync() =>
        await _context.Campaigns.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Campaign> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Campaigns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<CampaignStatus>(query.Status.Trim(), true, out var status))
            {
                itemsQuery = itemsQuery.Where(item => item.Status == status);
            }
            else
            {
                itemsQuery = itemsQuery.Where(_ => false);
            }
        }

        itemsQuery = ApplySort(itemsQuery, query);

        var totalCount = await itemsQuery.CountAsync();
        var items = await itemsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

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

    private static IOrderedQueryable<Campaign> ApplySort(IQueryable<Campaign> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "status" when descending => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            "status" => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            "startdate" when descending => query.OrderByDescending(item => item.StartDate).ThenByDescending(item => item.Id),
            "startdate" => query.OrderBy(item => item.StartDate).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
