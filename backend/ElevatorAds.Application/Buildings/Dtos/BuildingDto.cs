using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Buildings.Dtos;

public sealed record BuildingDto(
    Guid Id,
    string Name,
    string Address,
    string City,
    string Country,
    string PostalCode,
    BuildingType BuildingType,
    int EstimatedDailyAudience,
    DateTime CreatedAt,
    DateTime UpdatedAt);
