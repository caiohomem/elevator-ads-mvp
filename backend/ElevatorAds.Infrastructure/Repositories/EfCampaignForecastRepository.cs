using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfCampaignForecastRepository : ICampaignForecastRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfCampaignForecastRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<CampaignForecast?> GetByBookingRequestIdAsync(Guid bookingRequestId) =>
        await _context.CampaignForecasts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.BookingRequestId == bookingRequestId);

    public async Task<CampaignForecast> UpsertAsync(CampaignForecast forecast)
    {
        var existing = await _context.CampaignForecasts
            .FirstOrDefaultAsync(item => item.BookingRequestId == forecast.BookingRequestId);

        if (existing is null)
        {
            await _context.CampaignForecasts.AddAsync(forecast);
            await _context.SaveChangesAsync();
            return forecast;
        }

        existing.EligibleScreens = forecast.EligibleScreens;
        existing.EligibleBuildings = forecast.EligibleBuildings;
        existing.EstimatedPlays = forecast.EstimatedPlays;
        existing.EstimatedAudience = forecast.EstimatedAudience;
        existing.EstimatedCost = forecast.EstimatedCost;
        existing.AvailableCapacity = forecast.AvailableCapacity;
        existing.Warnings = forecast.Warnings;
        existing.Conflicts = forecast.Conflicts;
        existing.UpdatedAt = forecast.UpdatedAt;

        await _context.SaveChangesAsync();
        return existing;
    }
}
