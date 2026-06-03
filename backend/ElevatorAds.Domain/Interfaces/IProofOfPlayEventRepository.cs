using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface IProofOfPlayEventRepository
{
    Task<IEnumerable<ProofOfPlayEvent>> GetAllAsync();
    Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedByScreenIdAsync(Guid screenId, PagedQuery query);
    Task<(IEnumerable<ProofOfPlayEvent> Items, int TotalCount)> GetPagedByCampaignIdAsync(Guid campaignId, PagedQuery query);
    Task<ProofOfPlayEvent> AddAsync(ProofOfPlayEvent proofOfPlay);
    Task<IEnumerable<ProofOfPlayEvent>> GetByScreenIdAsync(Guid screenId);
    Task<IEnumerable<ProofOfPlayEvent>> GetByCampaignIdAsync(Guid campaignId);
    Task<IEnumerable<ProofOfPlayEvent>> GetByDateRangeAsync(DateTime from, DateTime to);
}
