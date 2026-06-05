namespace ElevatorAds.Application.BookingRequests.Dtos;

public sealed record CreateBookingRequestDto(
    Guid AdvertiserId,
    string Name,
    DateTime DateFrom,
    DateTime DateTo,
    List<string>? Cities,
    List<string>? BuildingTypes,
    List<string>? ScreenOrientations,
    int CreativeDurationSeconds,
    decimal Budget,
    string? CampaignObjective,
    string? Notes);
