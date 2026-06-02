using ElevatorAds.Application.Playlists.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Playlists;

public sealed class PlaylistDownloadService
{
    private readonly IDailyPlaylistRepository _playlistRepository;
    private readonly IScreenRepository _screenRepository;
    private readonly ICreativeRepository _creativeRepository;

    public PlaylistDownloadService(
        IDailyPlaylistRepository playlistRepository,
        IScreenRepository screenRepository,
        ICreativeRepository creativeRepository)
    {
        _playlistRepository = playlistRepository;
        _screenRepository = screenRepository;
        _creativeRepository = creativeRepository;
    }

    public Task<PlaylistDownloadDto?> GetCurrentAsync(Guid screenId) =>
        GetByDateAsync(screenId, DateOnly.FromDateTime(DateTime.UtcNow));

    public async Task<PlaylistDownloadDto?> GetByDateAsync(Guid screenId, DateOnly date)
    {
        if (await _screenRepository.GetByIdAsync(screenId) is null)
        {
            return null;
        }

        var playlist = await _playlistRepository.GetLatestPublishedByScreenAndDateAsync(screenId, date);
        return playlist is null ? null : await MapAsync(playlist);
    }

    public async Task<DownloadMarkerResult> MarkDownloadedAsync(Guid screenId, Guid playlistId)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.ScreenId != screenId)
        {
            return DownloadMarkerResult.NotFound();
        }

        if (playlist.Status != DailyPlaylistStatus.Published)
        {
            return DownloadMarkerResult.Invalid("Playlist must be Published before it can be marked as Downloaded.");
        }

        playlist.Status = DailyPlaylistStatus.Downloaded;
        playlist.UpdatedAt = DateTime.UtcNow;

        var updated = await _playlistRepository.UpdateAsync(playlist);
        return updated is null
            ? DownloadMarkerResult.NotFound()
            : DownloadMarkerResult.Success(await MapAsync(updated));
    }

    private async Task<PlaylistDownloadDto> MapAsync(DailyPlaylist playlist)
    {
        var items = new List<PlaylistDownloadItemDto>();

        foreach (var item in playlist.Items.OrderBy(item => item.Order))
        {
            var creative = await _creativeRepository.GetByIdAsync(item.CreativeId);
            items.Add(new PlaylistDownloadItemDto(
                item.Order,
                item.CampaignId,
                item.CreativeId,
                creative?.MediaUrl ?? string.Empty,
                creative?.MediaType.ToString() ?? string.Empty,
                item.DurationSeconds));
        }

        return new PlaylistDownloadDto(
            playlist.Id,
            playlist.ScreenId,
            playlist.Date,
            playlist.Version,
            playlist.Status.ToString(),
            playlist.GeneratedAt,
            playlist.PublishedAt,
            items);
    }

    public sealed record DownloadMarkerResult(bool IsSuccess, bool WasFound, string? Error, PlaylistDownloadDto? Value)
    {
        public static DownloadMarkerResult Success(PlaylistDownloadDto value) => new(true, true, null, value);

        public static DownloadMarkerResult NotFound() => new(false, false, null, null);

        public static DownloadMarkerResult Invalid(string error) => new(false, true, error, null);
    }
}
