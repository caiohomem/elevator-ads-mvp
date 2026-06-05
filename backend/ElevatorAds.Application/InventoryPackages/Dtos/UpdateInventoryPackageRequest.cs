using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.InventoryPackages.Dtos;

public sealed record UpdateInventoryPackageRequest(
    string Name,
    string? Description,
    List<string>? Cities,
    List<string>? BuildingTypes,
    List<string>? ScreenOrientations,
    List<Guid>? ScreenIds,
    List<Guid>? BuildingIds,
    decimal BaseCpm,
    InventoryPackageStatus Status);
