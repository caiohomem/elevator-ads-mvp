using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfBuildingRepository : IBuildingRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfBuildingRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Building>> GetAllAsync() =>
        await _context.Buildings.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Building>> GetByOrganizationAsync(Guid organizationId) =>
        await _context.Buildings
            .AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();

    public async Task<(IEnumerable<Building> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Buildings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        itemsQuery = ApplySort(itemsQuery, query);

        var totalCount = await itemsQuery.CountAsync();
        var items = await itemsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Building?> GetByIdAsync(Guid id) =>
        await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Building> AddAsync(Building building)
    {
        await _context.Buildings.AddAsync(building);
        await _context.SaveChangesAsync();
        return building;
    }

    public async Task<Building?> UpdateAsync(Building building)
    {
        var existing = await _context.Buildings.FirstOrDefaultAsync(item => item.Id == building.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(building);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Buildings.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Buildings.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    private static IOrderedQueryable<Building> ApplySort(IQueryable<Building> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "city" when descending => query.OrderByDescending(item => item.City).ThenByDescending(item => item.Id),
            "city" => query.OrderBy(item => item.City).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
