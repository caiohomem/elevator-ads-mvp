namespace ElevatorAds.Application.DeliveryReports.Dtos;

public sealed record CampaignReportDto(
    DateOnly From,
    DateOnly To,
    int TotalPlays,
    long TotalPlayedSeconds,
    IReadOnlyList<GroupSummaryDto> Campaigns);
