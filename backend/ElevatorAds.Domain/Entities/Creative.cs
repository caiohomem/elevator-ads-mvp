using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class Creative
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AdvertiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public int DurationSeconds { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Organization? Organization { get; set; }
}
