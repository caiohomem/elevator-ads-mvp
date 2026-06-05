namespace ElevatorAds.Application.Programmatic;

public sealed record SimulatorForecastResponse(
    int EligibleScreens,
    int EligibleBuildings,
    long EstimatedPlays,
    long EstimatedAudience,
    decimal EstimatedCost,
    decimal AvailableCapacity,
    List<string> Warnings,
    List<string> Conflicts,
    string SuggestedNextAction);
