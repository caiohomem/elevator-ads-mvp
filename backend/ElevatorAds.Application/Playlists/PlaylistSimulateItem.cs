namespace ElevatorAds.Application.Playlists;

public sealed record PlaylistSimulateItem(
    int Order,
    Guid? CampaignId,
    Guid? CreativeId,
    int CreativeDurationSeconds,
    string Source,
    string? Notes);
