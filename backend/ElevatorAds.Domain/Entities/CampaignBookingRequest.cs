using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class CampaignBookingRequest
{
    public Guid Id { get; set; }
    public Guid AdvertiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<string> Cities { get; set; } = [];
    public List<string> BuildingTypes { get; set; } = [];
    public List<string> ScreenOrientations { get; set; } = [];
    public int CreativeDurationSeconds { get; set; }
    public decimal Budget { get; set; }
    public string CampaignObjective { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
