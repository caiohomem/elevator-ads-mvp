using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Advertisers.Dtos;

public sealed record AdvertiserDto(
    Guid Id,
    string Name,
    string LegalName,
    string TaxId,
    string ContactName,
    string ContactEmail,
    string Phone,
    AdvertiserStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
