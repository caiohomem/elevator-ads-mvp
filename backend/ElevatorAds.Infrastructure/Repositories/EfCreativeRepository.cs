using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCreativeRepository : ICreativeRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCreativeRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Creative>> GetAllAsync() =>
        await _context.Creatives.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Creative> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Creatives.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<ApprovalStatus>(query.Status.Trim(), true, out var approvalStatus))
            {
                itemsQuery = itemsQuery.Where(item => item.ApprovalStatus == approvalStatus);
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

    public async Task<Creative?> GetByIdAsync(Guid id) =>
        await _context.Creatives.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Creative> AddAsync(Creative creative)
    {
        await _context.Creatives.AddAsync(creative);
        await _context.SaveChangesAsync();
        return creative;
    }

    public async Task<Creative?> UpdateAsync(Creative creative)
    {
        var existing = await _context.Creatives.FirstOrDefaultAsync(item => item.Id == creative.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(creative);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Creatives.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Creatives.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    private static IOrderedQueryable<Creative> ApplySort(IQueryable<Creative> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "approvalstatus" when descending => query.OrderByDescending(item => item.ApprovalStatus).ThenByDescending(item => item.Id),
            "approvalstatus" => query.OrderBy(item => item.ApprovalStatus).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
