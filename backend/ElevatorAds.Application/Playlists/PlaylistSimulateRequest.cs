namespace ElevatorAds.Application.Playlists;

public sealed record PlaylistSimulateRequest(
    Guid? BookingRequestId,
    Guid? CampaignId,
    Guid? InventoryPackageId,
    DateOnly Date,
    List<Guid>? ScreenIds,
    int CreativeDurationSeconds,
    double OperatingHoursPerDay,
    int? MaxLoopDurationSeconds);
