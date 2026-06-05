namespace ElevatorAds.Application.BookingRequests.Dtos;

public sealed record UpdateBookingRequestDto(
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
