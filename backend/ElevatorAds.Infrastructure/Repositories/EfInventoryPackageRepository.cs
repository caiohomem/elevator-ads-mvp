using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfInventoryPackageRepository : IInventoryPackageRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfInventoryPackageRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<(IEnumerable<InventoryPackage> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.InventoryPackages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<InventoryPackageStatus>(query.Status.Trim(), true, out var status))
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

    public async Task<InventoryPackage?> GetByIdAsync(Guid id) =>
        await _context.InventoryPackages.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<InventoryPackage> AddAsync(InventoryPackage inventoryPackage)
    {
        await _context.InventoryPackages.AddAsync(inventoryPackage);
        await _context.SaveChangesAsync();
        return inventoryPackage;
    }

    public async Task<InventoryPackage?> UpdateAsync(InventoryPackage inventoryPackage)
    {
        var existing = await _context.InventoryPackages.FirstOrDefaultAsync(item => item.Id == inventoryPackage.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(inventoryPackage);
        existing.Cities = inventoryPackage.Cities;
        existing.BuildingTypes = inventoryPackage.BuildingTypes;
        existing.ScreenOrientations = inventoryPackage.ScreenOrientations;
        existing.ScreenIds = inventoryPackage.ScreenIds;
        existing.BuildingIds = inventoryPackage.BuildingIds;
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.InventoryPackages.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.InventoryPackages.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    private static IOrderedQueryable<InventoryPackage> ApplySort(
        IQueryable<InventoryPackage> query,
        PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "status" when descending => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            "status" => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            "basecpm" when descending => query.OrderByDescending(item => item.BaseCpm).ThenByDescending(item => item.Id),
            "basecpm" => query.OrderBy(item => item.BaseCpm).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
