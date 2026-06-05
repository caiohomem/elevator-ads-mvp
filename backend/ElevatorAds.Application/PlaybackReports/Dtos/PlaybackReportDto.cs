namespace ElevatorAds.Application.PlaybackReports.Dtos;

public sealed record PlaybackReportDto(
    Guid Id,
    Guid ScreenId,
    string ScreenName,
    Guid PlaylistId,
    Guid PlaylistItemId,
    Guid CampaignId,
    string CampaignName,
    Guid CreativeId,
    string CreativeName,
    DateTime PlayedAt,
    int DurationSeconds,
    DateTime CreatedAt);
