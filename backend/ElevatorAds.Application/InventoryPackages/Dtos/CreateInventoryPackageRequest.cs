using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.InventoryPackages.Dtos;

public sealed record CreateInventoryPackageRequest(
    string Name,
    string? Description,
    List<string>? Cities,
    List<string>? BuildingTypes,
    List<string>? ScreenOrientations,
    List<Guid>? ScreenIds,
    List<Guid>? BuildingIds,
    decimal BaseCpm,
    InventoryPackageStatus Status);
