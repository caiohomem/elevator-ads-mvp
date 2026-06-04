namespace ElevatorAds.Domain.Entities;

public class ProofOfPlayEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ScreenId { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid PlaylistItemId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CreativeId { get; set; }
    public DateTime PlayedAt { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization? Organization { get; set; }
}
