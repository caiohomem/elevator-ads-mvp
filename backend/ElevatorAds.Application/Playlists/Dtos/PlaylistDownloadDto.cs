namespace ElevatorAds.Application.Playlists.Dtos;

public sealed record PlaylistDownloadDto(
    Guid PlaylistId,
    Guid ScreenId,
    DateOnly Date,
    int Version,
    string Status,
    DateTime GeneratedAt,
    DateTime? PublishedAt,
    IReadOnlyList<PlaylistDownloadItemDto> Items);
