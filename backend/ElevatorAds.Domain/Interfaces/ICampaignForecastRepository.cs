using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignForecastRepository
{
    Task<CampaignForecast?> GetByBookingRequestIdAsync(Guid bookingRequestId);
    Task<CampaignForecast> UpsertAsync(CampaignForecast forecast);
}
