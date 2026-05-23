using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryCreativeRepository : ICreativeRepository
{
    private readonly Dictionary<Guid, Creative> _creatives = new();

    public Task<IEnumerable<Creative>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Creative>>(_creatives.Values.OrderBy(creative => creative.CreatedAt).ToList());

    public Task<Creative?> GetByIdAsync(Guid id)
    {
        _creatives.TryGetValue(id, out var creative);
        return Task.FromResult(creative);
    }

    public Task<Creative> AddAsync(Creative creative)
    {
        _creatives[creative.Id] = creative;
        return Task.FromResult(creative);
    }

    public Task<Creative?> UpdateAsync(Creative creative)
    {
        if (!_creatives.ContainsKey(creative.Id))
        {
            return Task.FromResult<Creative?>(null);
        }

        _creatives[creative.Id] = creative;
        return Task.FromResult<Creative?>(creative);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_creatives.Remove(id));
}
