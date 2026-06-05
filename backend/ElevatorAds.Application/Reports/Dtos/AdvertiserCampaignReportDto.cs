namespace ElevatorAds.Application.Reports.Dtos;

public sealed record AdvertiserCampaignCreativeSummaryDto(
    Guid CreativeId,
    string CreativeName,
    string MediaType,
    int DurationSeconds,
    int TotalPlays,
    long EstimatedImpressions);

public sealed record AdvertiserCampaignDailyBreakdownDto(
    DateOnly Date,
    int TotalPlays,
    long EstimatedAudience,
    long EstimatedImpressions,
    int ScreensCount,
    int BuildingsCount);

public sealed record AdvertiserCampaignReportDto(
    Guid AdvertiserId,
    string AdvertiserName,
    Guid CampaignId,
    string CampaignName,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Status,
    int TotalPlays,
    int TotalScheduledPlays,
    int TotalReportedPlays,
    long EstimatedAudience,
    long EstimatedImpressions,
    int ScreensCount,
    int BuildingsCount,
    IReadOnlyList<string> Cities,
    IReadOnlyList<AdvertiserCampaignCreativeSummaryDto> Creatives,
    IReadOnlyList<AdvertiserCampaignDailyBreakdownDto> DailyBreakdown,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings);
