using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.InventoryPackages.Dtos;

public sealed record InventoryPackageDto(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<string> Cities,
    IReadOnlyList<string> BuildingTypes,
    IReadOnlyList<string> ScreenOrientations,
    IReadOnlyList<Guid> ScreenIds,
    IReadOnlyList<Guid> BuildingIds,
    decimal BaseCpm,
    InventoryPackageStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
