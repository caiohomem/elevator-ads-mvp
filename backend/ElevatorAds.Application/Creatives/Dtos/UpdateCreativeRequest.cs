using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Creatives.Dtos;

public sealed record UpdateCreativeRequest(
    string Name,
    string MediaUrl,
    MediaType MediaType,
    int DurationSeconds);
