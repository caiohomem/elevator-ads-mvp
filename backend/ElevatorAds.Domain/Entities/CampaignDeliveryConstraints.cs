using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class CampaignDeliveryConstraints
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public List<string> Cities { get; set; } = [];
    public List<BuildingType> BuildingTypes { get; set; } = [];
    public List<ScreenOrientation> ScreenOrientations { get; set; } = [];
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
