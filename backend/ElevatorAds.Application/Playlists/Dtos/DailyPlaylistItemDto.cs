namespace ElevatorAds.Application.Playlists.Dtos;

public sealed record DailyPlaylistItemDto(
    Guid Id,
    Guid DailyPlaylistId,
    Guid CampaignId,
    Guid CreativeId,
    int Order,
    int DurationSeconds,
    DateTime CreatedAt);
