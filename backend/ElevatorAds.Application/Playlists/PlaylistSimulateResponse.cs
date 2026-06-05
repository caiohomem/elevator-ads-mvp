namespace ElevatorAds.Application.Playlists;

public sealed record PlaylistSimulateResponse(
    DateOnly Date,
    int EligibleScreens,
    int EligibleBuildings,
    int LoopDurationSeconds,
    double EstimatedLoopsPerDay,
    double EstimatedPlaysPerCreative,
    long EstimatedTotalPlays,
    long EstimatedAudience,
    List<PlaylistSimulateItem> Items,
    List<string> Warnings,
    List<string> Conflicts);
