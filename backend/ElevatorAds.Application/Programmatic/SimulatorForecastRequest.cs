namespace ElevatorAds.Application.Programmatic;

public sealed record SimulatorForecastRequest(
    string? AdvertiserId,
    DateOnly DateFrom,
    DateOnly DateTo,
    List<string>? Cities,
    List<string>? BuildingTypes,
    List<string>? ScreenOrientations,
    int CreativeDurationSeconds,
    decimal? Budget,
    string? CampaignObjective,
    string? Notes);
