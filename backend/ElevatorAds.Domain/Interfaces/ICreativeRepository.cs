using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface ICreativeRepository
{
    Task<IEnumerable<Creative>> GetAllAsync();
    Task<(IEnumerable<Creative> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Creative?> GetByIdAsync(Guid id);
    Task<Creative> AddAsync(Creative creative);
    Task<Creative?> UpdateAsync(Creative creative);
    Task<bool> DeleteAsync(Guid id);
}
