namespace ElevatorAds.Application.Reports.Dtos;

public sealed record EstimatedProofOfPlayReportDto(
    Guid CampaignId,
    string CampaignName,
    Guid AdvertiserId,
    string AdvertiserName,
    DateOnly DateFrom,
    DateOnly DateTo,
    int TotalScheduledPlays,
    int TotalReportedPlays,
    long EstimatedAudience,
    long EstimatedImpressions,
    int ScreensCount,
    int BuildingsCount,
    IReadOnlyList<string> Cities,
    IReadOnlyList<EstimatedProofOfPlayItemDto> Items,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings);
