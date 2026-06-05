using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignBookingRequestRepository : ICampaignBookingRequestRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignBookingRequestRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<CampaignBookingRequest>> GetAllAsync() =>
        await _context.CampaignBookingRequests.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<CampaignBookingRequest> Items, int TotalCount)> GetPagedAsync(PagedQuery query)
    {
        var itemsQuery = _context.CampaignBookingRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(item => item.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<BookingRequestStatus>(query.Status.Trim(), true, out var status))
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

    public async Task<CampaignBookingRequest?> GetByIdAsync(Guid id) =>
        await _context.CampaignBookingRequests.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    public async Task<CampaignBookingRequest> AddAsync(CampaignBookingRequest bookingRequest)
    {
        await _context.CampaignBookingRequests.AddAsync(bookingRequest);
        await _context.SaveChangesAsync();
        return bookingRequest;
    }

    public async Task<CampaignBookingRequest?> UpdateAsync(CampaignBookingRequest bookingRequest)
    {
        var existing = await _context.CampaignBookingRequests.FirstOrDefaultAsync(item => item.Id == bookingRequest.Id);
        if (existing is null)
        {
            return null;
        }

        _context.Entry(existing).CurrentValues.SetValues(bookingRequest);
        existing.Cities = bookingRequest.Cities;
        existing.BuildingTypes = bookingRequest.BuildingTypes;
        existing.ScreenOrientations = bookingRequest.ScreenOrientations;
        await _context.SaveChangesAsync();
        return existing;
    }

    private static IOrderedQueryable<CampaignBookingRequest> ApplySort(
        IQueryable<CampaignBookingRequest> query,
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
            "createdat" when descending => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            "createdat" => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }
}
