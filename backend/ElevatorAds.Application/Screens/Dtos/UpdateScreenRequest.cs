using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Screens.Dtos;

public sealed record UpdateScreenRequest(
    Guid BuildingId,
    string Name,
    string ExternalCode,
    int ResolutionWidth,
    int ResolutionHeight,
    ScreenOrientation Orientation,
    ScreenStatus Status);
