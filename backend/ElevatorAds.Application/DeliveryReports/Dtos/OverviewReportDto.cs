namespace ElevatorAds.Application.DeliveryReports.Dtos;

public sealed record OverviewReportDto(
    DateOnly Date,
    int TotalPlays,
    long TotalPlayedSeconds,
    IReadOnlyList<GroupSummaryDto> ByCampaign,
    IReadOnlyList<GroupSummaryDto> ByScreen,
    IReadOnlyList<GroupSummaryDto> ByCreative);
