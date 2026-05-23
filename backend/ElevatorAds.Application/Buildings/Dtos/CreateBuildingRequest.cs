using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Buildings.Dtos;

public sealed record CreateBuildingRequest(
    string Name,
    string Address,
    string City,
    string Country,
    string PostalCode,
    BuildingType BuildingType,
    int EstimatedDailyAudience);
