using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Advertisers.Dtos;

public sealed record UpdateAdvertiserRequest(
    string Name,
    string LegalName,
    string TaxId,
    string ContactName,
    string ContactEmail,
    string Phone,
    AdvertiserStatus Status);
