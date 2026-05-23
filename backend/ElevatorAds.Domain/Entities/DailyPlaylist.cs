using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public class DailyPlaylist
{
    public Guid Id { get; set; }
    public Guid ScreenId { get; set; }
    public DateOnly Date { get; set; }
    public int Version { get; set; }
    public DailyPlaylistStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DailyPlaylistItem> Items { get; set; } = [];
}
