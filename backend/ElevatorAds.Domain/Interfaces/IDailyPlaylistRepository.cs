using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IDailyPlaylistRepository
{
    Task<IEnumerable<DailyPlaylist>> GetAllAsync();
    Task<DailyPlaylist?> GetByIdAsync(Guid id);
    Task<DailyPlaylist?> GetByScreenAndDateAsync(Guid screenId, DateOnly date);
    Task<DailyPlaylist?> GetLatestPublishedByScreenAndDateAsync(Guid screenId, DateOnly date);
    Task<IEnumerable<DailyPlaylist>> GetByScreenIdAsync(Guid screenId, DateOnly? date);
    Task<DailyPlaylist> AddAsync(DailyPlaylist playlist);
    Task<DailyPlaylist?> UpdateAsync(DailyPlaylist playlist);
}
