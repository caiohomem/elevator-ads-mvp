namespace ElevatorAds.Domain.Entities;

public class DailyPlaylistItem
{
    public Guid Id { get; set; }
    public Guid DailyPlaylistId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CreativeId { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}
