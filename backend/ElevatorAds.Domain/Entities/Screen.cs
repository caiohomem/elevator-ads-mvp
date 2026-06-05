using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class Screen
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalCode { get; set; } = string.Empty;
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
    public ScreenOrientation Orientation { get; set; }
    public ScreenStatus Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Organization? Organization { get; set; }
    public Building? Building { get; set; }
}
