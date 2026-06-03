using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfAdvertiserRepository : IAdvertiserRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfAdvertiserRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Advertiser>> GetAllAsync() =>
        await _context.Advertisers.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Advertiser> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Advertisers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<AdvertiserStatus>(query.Status.Trim(), true, out var status))
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

    private static IOrderedQueryable<Advertiser> ApplySort(IQueryable<Advertiser> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "status" when descending => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            "status" => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
