using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class InMemoryDailyPlaylistRepository : IDailyPlaylistRepository
{
    private readonly Dictionary<Guid, DailyPlaylist> _playlists = new();

    public Task<IEnumerable<DailyPlaylist>> GetAllAsync() =>
        Task.FromResult<IEnumerable<DailyPlaylist>>(
            _playlists.Values
                .OrderBy(playlist => playlist.Date)
                .ThenBy(playlist => playlist.ScreenId)
                .ThenBy(playlist => playlist.CreatedAt)
                .ToList());

    public Task<DailyPlaylist?> GetByIdAsync(Guid id)
    {
        _playlists.TryGetValue(id, out var playlist);
        return Task.FromResult(playlist);
    }

    public Task<DailyPlaylist?> GetByScreenAndDateAsync(Guid screenId, DateOnly date)
    {
        var playlist = _playlists.Values
            .Where(item => item.ScreenId == screenId && item.Date == date)
            .OrderByDescending(item => item.Version)
            .FirstOrDefault();

        return Task.FromResult(playlist);
    }

    public Task<IEnumerable<DailyPlaylist>> GetByScreenIdAsync(Guid screenId, DateOnly? date)
    {
        var playlists = _playlists.Values
            .Where(item => item.ScreenId == screenId && (!date.HasValue || item.Date == date.Value))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Version)
            .ToList();

        return Task.FromResult<IEnumerable<DailyPlaylist>>(playlists);
    }

    public Task<DailyPlaylist> AddAsync(DailyPlaylist playlist)
    {
        _playlists[playlist.Id] = playlist;
        return Task.FromResult(playlist);
    }

    public Task<DailyPlaylist?> UpdateAsync(DailyPlaylist playlist)
    {
        if (!_playlists.ContainsKey(playlist.Id))
        {
            return Task.FromResult<DailyPlaylist?>(null);
        }

        playlist.UpdatedAt = DateTime.UtcNow;
        _playlists[playlist.Id] = playlist;
        return Task.FromResult<DailyPlaylist?>(playlist);
    }
}
