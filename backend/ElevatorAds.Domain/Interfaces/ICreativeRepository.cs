using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface ICreativeRepository
{
    Task<IEnumerable<Creative>> GetAllAsync();
    Task<Creative?> GetByIdAsync(Guid id);
    Task<Creative> AddAsync(Creative creative);
    Task<Creative?> UpdateAsync(Creative creative);
    Task<bool> DeleteAsync(Guid id);
}
