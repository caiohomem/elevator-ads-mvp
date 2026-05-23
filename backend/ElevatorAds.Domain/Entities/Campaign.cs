using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; }
    public Guid AdvertiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public decimal? DailyBudget { get; set; }
    public decimal? TotalBudget { get; set; }
    public decimal? MaxCpm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
