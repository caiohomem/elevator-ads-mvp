using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICampaignBookingRequestRepository
{
    Task<IEnumerable<CampaignBookingRequest>> GetAllAsync();
    Task<(IEnumerable<CampaignBookingRequest> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<CampaignBookingRequest?> GetByIdAsync(Guid id);
    Task<CampaignBookingRequest> AddAsync(CampaignBookingRequest bookingRequest);
    Task<CampaignBookingRequest?> UpdateAsync(CampaignBookingRequest bookingRequest);
}
