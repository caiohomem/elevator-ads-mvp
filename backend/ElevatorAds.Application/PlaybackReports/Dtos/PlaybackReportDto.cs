namespace ElevatorAds.Application.PlaybackReports.Dtos;

public sealed record PlaybackReportDto(
    Guid Id,
    Guid ScreenId,
    Guid PlaylistId,
    Guid PlaylistItemId,
    Guid CampaignId,
    Guid CreativeId,
    DateTime PlayedAt,
    int DurationSeconds,
    DateTime CreatedAt);
