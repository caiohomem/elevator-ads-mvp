using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.BookingRequests.Dtos;

public sealed record BookingRequestDto(
    Guid Id,
    Guid AdvertiserId,
    string Name,
    DateTime DateFrom,
    DateTime DateTo,
    IReadOnlyList<string> Cities,
    IReadOnlyList<string> BuildingTypes,
    IReadOnlyList<string> ScreenOrientations,
    int CreativeDurationSeconds,
    decimal Budget,
    string CampaignObjective,
    string Notes,
    BookingRequestStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
