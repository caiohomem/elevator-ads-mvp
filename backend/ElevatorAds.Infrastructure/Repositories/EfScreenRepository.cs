using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfScreenRepository : IScreenRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfScreenRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Screen>> GetAllAsync() =>
        await _context.Screens.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Screen> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Screens.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<ScreenStatus>(query.Status.Trim(), true, out var status))
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

    public async Task<Screen?> GetByIdAsync(Guid id) =>
        await _context.Screens.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Screen> AddAsync(Screen screen)
    {
        await _context.Screens.AddAsync(screen);
        await _context.SaveChangesAsync();
        return screen;
    }

    public async Task<Screen?> UpdateAsync(Screen screen)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == screen.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(screen);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Screens.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Screen?> UpdateLastSeenAtAsync(Guid id, DateTime lastSeenAt)
    {
        var existing = await _context.Screens.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.LastSeenAt = lastSeenAt;
        existing.UpdatedAt = lastSeenAt;
        await _context.SaveChangesAsync();
        return existing;
    }

    private static IOrderedQueryable<Screen> ApplySort(IQueryable<Screen> query, PagedQuery pagedQuery)
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
