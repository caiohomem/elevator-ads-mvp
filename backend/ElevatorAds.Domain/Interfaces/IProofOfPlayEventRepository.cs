using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IProofOfPlayEventRepository
{
    Task<IEnumerable<ProofOfPlayEvent>> GetAllAsync();
    Task<ProofOfPlayEvent> AddAsync(ProofOfPlayEvent proofOfPlay);
    Task<IEnumerable<ProofOfPlayEvent>> GetByScreenIdAsync(Guid screenId);
    Task<IEnumerable<ProofOfPlayEvent>> GetByCampaignIdAsync(Guid campaignId);
    Task<IEnumerable<ProofOfPlayEvent>> GetByDateRangeAsync(DateTime from, DateTime to);
}
