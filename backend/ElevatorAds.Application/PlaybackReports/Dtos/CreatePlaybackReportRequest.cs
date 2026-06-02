namespace ElevatorAds.Application.PlaybackReports.Dtos;

public sealed record CreatePlaybackReportRequest(
    Guid PlaylistId,
    Guid PlaylistItemId,
    DateTime? PlayedAt,
    int DurationSeconds);
