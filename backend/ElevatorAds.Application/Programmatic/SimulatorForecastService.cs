using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Programmatic;

public sealed class SimulatorForecastService
{
    private const decimal BaseCpm = 10.00m;
    private const decimal FullAvailableCapacity = 1.0m;
    private const int PlaylistLoopSeconds = 480;
    private const string MissingAudienceWarning = "Some eligible buildings are missing audience data. Audience estimates may be understated.";
    private const string ApproximateCostWarning = "Estimated cost uses a placeholder CPM for the MVP forecast.";
    private const string NoMatchingScreensWarning = "No screens matched the selected filters.";
    private const string PastDateWarning = "The requested date range ends in the past. Forecast values are still calculated for comparison only.";
    private const string BudgetConflict = "Estimated cost exceeds the provided budget.";
    private const string SuggestedNextAction = "Contact sales to convert this forecast into a scheduled playlist campaign.";

    private readonly IBuildingRepository _buildingRepository;
    private readonly IScreenRepository _screenRepository;
    private readonly TimeProvider _timeProvider;

    public SimulatorForecastService(
        IBuildingRepository buildingRepository,
        IScreenRepository screenRepository,
        TimeProvider timeProvider)
    {
        _buildingRepository = buildingRepository;
        _screenRepository = screenRepository;
        _timeProvider = timeProvider;
    }

    public async Task<SimulatorForecastResponse> ForecastAsync(
        SimulatorForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buildings = (await _buildingRepository.GetAllAsync()).ToDictionary(item => item.Id);
        var screens = await _screenRepository.GetAllAsync();

        var cityFilters = NormalizeSet(request.Cities);
        var buildingTypeFilters = NormalizeSet(request.BuildingTypes);
        var orientationFilters = NormalizeSet(request.ScreenOrientations);

        var eligibleScreens = screens
            .Where(screen => screen.Status == ScreenStatus.Active)
            .Where(screen => buildings.ContainsKey(screen.BuildingId))
            .Where(screen =>
            {
                var building = buildings[screen.BuildingId];
                return Matches(cityFilters, building.City)
                    && Matches(buildingTypeFilters, building.BuildingType.ToString())
                    && Matches(orientationFilters, screen.Orientation.ToString());
            })
            .ToList();

        var eligibleBuildings = eligibleScreens
            .Select(screen => buildings[screen.BuildingId])
            .DistinctBy(building => building.Id)
            .ToList();

        var rangeDays = request.DateTo.DayNumber - request.DateFrom.DayNumber + 1;
        var playsPerScreenPerDay = Math.Max(1, PlaylistLoopSeconds / request.CreativeDurationSeconds);
        var estimatedPlays = (long)eligibleScreens.Count * playsPerScreenPerDay * rangeDays;
        var estimatedAudience = eligibleBuildings.Sum(item => (long)item.EstimatedDailyAudience) * rangeDays;
        var estimatedCost = decimal.Round((estimatedPlays / 1000m) * BaseCpm, 2, MidpointRounding.AwayFromZero);

        var warnings = new List<string>();
        var conflicts = new List<string>();

        if (eligibleScreens.Count == 0)
        {
            warnings.Add(NoMatchingScreensWarning);
        }

        if (eligibleBuildings.Any(item => item.EstimatedDailyAudience <= 0))
        {
            warnings.Add(MissingAudienceWarning);
        }

        warnings.Add(ApproximateCostWarning);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (request.DateTo < today)
        {
            warnings.Add(PastDateWarning);
        }

        if (request.Budget is decimal budget && budget < estimatedCost)
        {
            conflicts.Add(BudgetConflict);
        }

        return new SimulatorForecastResponse(
            eligibleScreens.Count,
            eligibleBuildings.Count,
            estimatedPlays,
            estimatedAudience,
            estimatedCost,
            FullAvailableCapacity,
            warnings,
            conflicts,
            SuggestedNextAction);
    }

    private static HashSet<string>? NormalizeSet(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Select(value => value.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return normalized.Count == 0 ? null : normalized;
    }

    private static bool Matches(HashSet<string>? filters, string candidate) =>
        filters is null || filters.Contains(candidate.Trim().ToLowerInvariant());
}
