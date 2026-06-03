using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Domain.Interfaces;

public interface IDailyPlaylistRepository
{
    Task<IEnumerable<DailyPlaylist>> GetAllAsync();
    Task<(IEnumerable<DailyPlaylist> Items, int TotalCount)> GetPagedAsync(PagedQuery query);
    Task<DailyPlaylist?> GetByIdAsync(Guid id);
    Task<DailyPlaylist?> GetByScreenAndDateAsync(Guid screenId, DateOnly date);
    Task<DailyPlaylist?> GetLatestPublishedByScreenAndDateAsync(Guid screenId, DateOnly date);
    Task<IEnumerable<DailyPlaylist>> GetByScreenIdAsync(Guid screenId, DateOnly? date);
    Task<DailyPlaylist> AddAsync(DailyPlaylist playlist);
    Task<DailyPlaylist?> UpdateAsync(DailyPlaylist playlist);
}
