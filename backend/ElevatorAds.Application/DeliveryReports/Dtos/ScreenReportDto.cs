namespace ElevatorAds.Application.DeliveryReports.Dtos;

public sealed record ScreenReportDto(
    DateOnly From,
    DateOnly To,
    int TotalPlays,
    long TotalPlayedSeconds,
    IReadOnlyList<GroupSummaryDto> Screens);
