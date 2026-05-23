using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Playlists.Dtos;

public sealed record DailyPlaylistDto(
    Guid Id,
    Guid ScreenId,
    DateOnly Date,
    int Version,
    DailyPlaylistStatus Status,
    DateTime GeneratedAt,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DailyPlaylistItemDto> Items);
