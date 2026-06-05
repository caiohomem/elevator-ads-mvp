namespace ElevatorAds.Application.Reports.Dtos;

public sealed record EstimatedProofOfPlayItemDto(
    DateOnly Date,
    Guid ScreenId,
    string ScreenName,
    Guid BuildingId,
    string BuildingName,
    string City,
    Guid CreativeId,
    string CreativeName,
    int ScheduledPlays,
    int ReportedPlays,
    long EstimatedAudience,
    long EstimatedImpressions);
