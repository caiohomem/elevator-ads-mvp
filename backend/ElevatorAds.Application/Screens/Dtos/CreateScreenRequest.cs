using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Screens.Dtos;

public sealed record CreateScreenRequest(
    Guid BuildingId,
    string Name,
    string ExternalCode,
    int ResolutionWidth,
    int ResolutionHeight,
    ScreenOrientation Orientation,
    ScreenStatus Status);
