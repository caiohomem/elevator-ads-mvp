namespace ElevatorAds.Application.Organizations.Dtos;

public sealed record UpdateOrganizationRequest(
    string Name,
    string Slug,
    string? Status);
