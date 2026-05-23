namespace ElevatorAds.Domain.Entities;

public class CampaignCreative
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CreativeId { get; set; }
    public DateTime CreatedAt { get; set; }
}
