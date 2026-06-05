using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IInventoryPackageRepository
{
    Task<(IEnumerable<InventoryPackage> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<InventoryPackage?> GetByIdAsync(Guid id);
    Task<InventoryPackage> AddAsync(InventoryPackage inventoryPackage);
    Task<InventoryPackage?> UpdateAsync(InventoryPackage inventoryPackage);
    Task<bool> DeleteAsync(Guid id);
}
