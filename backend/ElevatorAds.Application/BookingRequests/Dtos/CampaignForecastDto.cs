namespace ElevatorAds.Application.BookingRequests.Dtos;

public sealed record CampaignForecastDto(
    Guid Id,
    Guid BookingRequestId,
    int EligibleScreens,
    int EligibleBuildings,
    long EstimatedPlays,
    long EstimatedAudience,
    decimal EstimatedCost,
    decimal AvailableCapacity,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Conflicts,
    DateTime CreatedAt,
    DateTime UpdatedAt);
