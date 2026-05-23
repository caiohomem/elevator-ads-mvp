using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Creatives.Dtos;

public sealed record CreateCreativeRequest(
    Guid AdvertiserId,
    string Name,
    string MediaUrl,
    MediaType MediaType,
    int DurationSeconds);
