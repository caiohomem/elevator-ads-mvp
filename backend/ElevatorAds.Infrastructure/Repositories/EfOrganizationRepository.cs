using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfOrganizationRepository : IOrganizationRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfOrganizationRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<Organization>> GetAllAsync() =>
        await _context.Organizations.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Organization> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.Organizations.AsNoTracking();

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

    public async Task<Organization?> GetByIdAsync(Guid id) =>
        await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<Organization?> GetBySlugAsync(string slug) =>
        await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(item => item.Slug == slug);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
    {
        var query = _context.Organizations.AsNoTracking().Where(item => item.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(item => item.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Organization> AddAsync(Organization organization)
    {
        await _context.Organizations.AddAsync(organization);
        await _context.SaveChangesAsync();
        return organization;
    }

    public async Task<Organization?> UpdateAsync(Organization organization)
    {
        var existing = await _context.Organizations.FirstOrDefaultAsync(item => item.Id == organization.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(organization);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Organizations.FirstOrDefaultAsync(item => item.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.Organizations.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> EnsureDefaultOrganizationIdAsync(string defaultName, string defaultSlug, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Slug == defaultSlug, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var now = DateTime.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = defaultName,
            Slug = defaultSlug,
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _context.Organizations.AddAsync(org, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return org.Id;
        }
        catch (DbUpdateException)
        {
            // Concurrent insert race: another process created the default org first.
            var retry = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Slug == defaultSlug, cancellationToken);

            return retry?.Id ?? org.Id;
        }
    }

    private static IOrderedQueryable<Organization> ApplySort(IQueryable<Organization> query, PagedQuery pagedQuery)
    {
        var sortBy = pagedQuery.SortBy?.Trim().ToLowerInvariant();
        var descending = string.Equals(pagedQuery.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" when descending => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            "name" => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "slug" when descending => query.OrderByDescending(item => item.Slug).ThenByDescending(item => item.Id),
            "slug" => query.OrderBy(item => item.Slug).ThenBy(item => item.Id),
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
