using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Screens.Dtos;

public sealed record ScreenDto(
    Guid Id,
    Guid BuildingId,
    string Name,
    string ExternalCode,
    int ResolutionWidth,
    int ResolutionHeight,
    ScreenOrientation Orientation,
    ScreenStatus Status,
    DateTime? LastSeenAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
