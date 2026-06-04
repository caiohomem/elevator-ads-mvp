namespace ElevatorAds.Application.Organizations.Dtos;

public sealed record CreateOrganizationRequest(
    string Name,
    string Slug,
    string? Status);
