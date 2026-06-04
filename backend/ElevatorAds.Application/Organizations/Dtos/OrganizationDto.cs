namespace ElevatorAds.Application.Organizations.Dtos;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
