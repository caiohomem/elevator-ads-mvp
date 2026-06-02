using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryProofOfPlayEventRepository : IProofOfPlayEventRepository
{
    private readonly Dictionary<Guid, ProofOfPlayEvent> _events = new();

    public Task<IEnumerable<ProofOfPlayEvent>> GetAllAsync() =>
        Task.FromResult<IEnumerable<ProofOfPlayEvent>>(
            _events.Values
                .OrderByDescending(item => item.PlayedAt)
                .ThenByDescending(item => item.CreatedAt)
                .ToList());

    public Task<ProofOfPlayEvent> AddAsync(ProofOfPlayEvent proofOfPlay)
    {
        _events[proofOfPlay.Id] = proofOfPlay;
        return Task.FromResult(proofOfPlay);
    }

    public Task<IEnumerable<ProofOfPlayEvent>> GetByScreenIdAsync(Guid screenId) =>
        Task.FromResult<IEnumerable<ProofOfPlayEvent>>(
            _events.Values
                .Where(item => item.ScreenId == screenId)
                .OrderByDescending(item => item.PlayedAt)
                .ThenByDescending(item => item.CreatedAt)
                .ToList());

    public Task<IEnumerable<ProofOfPlayEvent>> GetByCampaignIdAsync(Guid campaignId) =>
        Task.FromResult<IEnumerable<ProofOfPlayEvent>>(
            _events.Values
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.PlayedAt)
                .ThenByDescending(item => item.CreatedAt)
                .ToList());
}
