namespace ElevatorAds.Domain.Entities;

public class CampaignForecast
{
    public Guid Id { get; set; }
    public Guid BookingRequestId { get; set; }
    public int EligibleScreens { get; set; }
    public int EligibleBuildings { get; set; }
    public long EstimatedPlays { get; set; }
    public long EstimatedAudience { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AvailableCapacity { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Conflicts { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
