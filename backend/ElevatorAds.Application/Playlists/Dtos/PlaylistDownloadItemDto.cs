namespace ElevatorAds.Application.Playlists.Dtos;

public sealed record PlaylistDownloadItemDto(
    int Order,
    Guid CampaignId,
    Guid CreativeId,
    string MediaUrl,
    string MediaType,
    int DurationSeconds);
