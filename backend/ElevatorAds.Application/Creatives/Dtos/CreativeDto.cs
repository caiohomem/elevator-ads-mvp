using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Creatives.Dtos;

public sealed record CreativeDto(
    Guid Id,
    Guid AdvertiserId,
    string Name,
    string MediaUrl,
    MediaType MediaType,
    int DurationSeconds,
    ApprovalStatus ApprovalStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt);
