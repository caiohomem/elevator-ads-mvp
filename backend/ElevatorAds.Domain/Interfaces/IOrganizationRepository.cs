using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<IEnumerable<Organization>> GetAllAsync();
    Task<(IEnumerable<Organization> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<Organization?> GetByIdAsync(Guid id);
    Task<Organization?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
    Task<Organization> AddAsync(Organization organization);
    Task<Organization?> UpdateAsync(Organization organization);
    Task<bool> DeleteAsync(Guid id);

    Task<Guid> EnsureDefaultOrganizationIdAsync(string defaultName, string defaultSlug, CancellationToken cancellationToken = default);
}
