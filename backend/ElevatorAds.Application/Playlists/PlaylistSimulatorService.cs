using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Playlists;

public sealed class PlaylistSimulatorService
{
    private const string MissingAudienceWarning = "Some eligible buildings are missing audience data. Audience estimates may be understated.";
    private const string NoMatchingScreensWarning = "No screens matched the selected filters.";
    private const string NoSourceWarning = "Simulation ran without a booking request, campaign, or inventory package. Active screens were used directly.";
    private const string NoCampaignConstraintsWarning = "No delivery constraints found for the campaign; all active screens were considered.";
    private const string TimeWindowWarning = "Campaign time-of-day constraints are not reflected in the MVP playlist simulator.";

    private readonly ICampaignBookingRequestRepository _bookingRequestRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignDeliveryConstraintsRepository _campaignDeliveryConstraintsRepository;
    private readonly IInventoryPackageRepository _inventoryPackageRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IScreenRepository _screenRepository;

    public PlaylistSimulatorService(
        ICampaignBookingRequestRepository bookingRequestRepository,
        ICampaignRepository campaignRepository,
        ICampaignDeliveryConstraintsRepository campaignDeliveryConstraintsRepository,
        IInventoryPackageRepository inventoryPackageRepository,
        IBuildingRepository buildingRepository,
        IScreenRepository screenRepository)
    {
        _bookingRequestRepository = bookingRequestRepository;
        _campaignRepository = campaignRepository;
        _campaignDeliveryConstraintsRepository = campaignDeliveryConstraintsRepository;
        _inventoryPackageRepository = inventoryPackageRepository;
        _buildingRepository = buildingRepository;
        _screenRepository = screenRepository;
    }

    public async Task<PlaylistSimulateResponse> SimulateAsync(
        PlaylistSimulateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>();
        var conflicts = new List<string>();
        var context = await ResolveContextAsync(request, warnings, conflicts, cancellationToken);

        var buildingsById = (await _buildingRepository.GetAllAsync()).ToDictionary(item => item.Id);
        var activeScreens = (await _screenRepository.GetAllAsync())
            .Where(screen => screen.Status == ScreenStatus.Active)
            .Where(screen => buildingsById.ContainsKey(screen.BuildingId))
            .ToList();

        var eligibleScreens = FilterScreens(activeScreens, buildingsById, context);

        if (request.ScreenIds is { Count: > 0 })
        {
            var selectedIds = request.ScreenIds.ToHashSet();
            eligibleScreens = activeScreens.Where(screen => selectedIds.Contains(screen.Id)).ToList();
        }

        var eligibleBuildings = eligibleScreens
            .Select(screen => buildingsById[screen.BuildingId])
            .DistinctBy(building => building.Id)
            .ToList();

        if (eligibleScreens.Count == 0)
        {
            warnings.Add(NoMatchingScreensWarning);
        }

        if (eligibleBuildings.Any(building => building.EstimatedDailyAudience <= 0))
        {
            warnings.Add(MissingAudienceWarning);
        }

        var items = new List<PlaylistSimulateItem>
        {
            new(
                1,
                context.CampaignId,
                null,
                request.CreativeDurationSeconds,
                context.Source,
                context.ItemNotes)
        };

        var loopDurationSeconds = items.Sum(item => item.CreativeDurationSeconds);
        var estimatedLoopsPerDay = loopDurationSeconds > 0
            ? Math.Floor((request.OperatingHoursPerDay * 3600d) / loopDurationSeconds)
            : 0d;
        var itemOccurrences = items.Count(item => item.CreativeId is null);
        var estimatedPlaysPerCreative = estimatedLoopsPerDay * itemOccurrences;
        var estimatedTotalPlays = (long)estimatedPlaysPerCreative * eligibleScreens.Count;
        var estimatedAudience = eligibleBuildings.Sum(building => (long)building.EstimatedDailyAudience);

        if (request.MaxLoopDurationSeconds is int maxLoopDurationSeconds && loopDurationSeconds > maxLoopDurationSeconds)
        {
            conflicts.Add("The simulated loop exceeds the requested maximum loop duration.");
        }

        return new PlaylistSimulateResponse(
            request.Date,
            eligibleScreens.Count,
            eligibleBuildings.Count,
            loopDurationSeconds,
            estimatedLoopsPerDay,
            estimatedPlaysPerCreative,
            estimatedTotalPlays,
            estimatedAudience,
            items,
            warnings,
            conflicts);
    }

    private async Task<SimulationContext> ResolveContextAsync(
        PlaylistSimulateRequest request,
        List<string> warnings,
        List<string> conflicts,
        CancellationToken cancellationToken)
    {
        if (request.BookingRequestId is Guid bookingRequestId)
        {
            var bookingRequest = await _bookingRequestRepository.GetByIdAsync(bookingRequestId);
            if (bookingRequest is null)
            {
                throw new KeyNotFoundException("Booking request not found.");
            }

            if (request.Date < DateOnly.FromDateTime(bookingRequest.DateFrom) ||
                request.Date > DateOnly.FromDateTime(bookingRequest.DateTo))
            {
                conflicts.Add("The selected date is outside the booking request flight.");
            }

            if (bookingRequest.CreativeDurationSeconds != request.CreativeDurationSeconds)
            {
                warnings.Add("Creative duration was overridden from the booking request default.");
            }

            return new SimulationContext(
                "BookingRequest",
                bookingRequestId,
                NormalizeSet(bookingRequest.Cities),
                NormalizeSet(bookingRequest.BuildingTypes),
                NormalizeSet(bookingRequest.ScreenOrientations),
                null,
                null,
                "Based on booking request targeting.");
        }

        if (request.CampaignId is Guid campaignId)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign is null)
            {
                throw new KeyNotFoundException("Campaign not found.");
            }

            if (campaign.StartDate.HasValue && request.Date < DateOnly.FromDateTime(campaign.StartDate.Value) ||
                campaign.EndDate.HasValue && request.Date > DateOnly.FromDateTime(campaign.EndDate.Value))
            {
                conflicts.Add("The selected date is outside the campaign flight.");
            }

            var constraints = await _campaignDeliveryConstraintsRepository.GetByCampaignIdAsync(campaignId);
            if (constraints is null)
            {
                warnings.Add(NoCampaignConstraintsWarning);
                return new SimulationContext("Campaign", campaignId, null, null, null, null, null, "Based on campaign with no delivery constraints.");
            }

            if (constraints.DaysOfWeek.Count > 0 && !constraints.DaysOfWeek.Contains(request.Date.DayOfWeek))
            {
                conflicts.Add("The selected date is outside the campaign delivery days.");
            }

            if (constraints.StartTime.HasValue || constraints.EndTime.HasValue)
            {
                warnings.Add(TimeWindowWarning);
            }

            return new SimulationContext(
                "Campaign",
                campaignId,
                NormalizeSet(constraints.Cities),
                NormalizeSet(constraints.BuildingTypes.Select(item => item.ToString())),
                NormalizeSet(constraints.ScreenOrientations.Select(item => item.ToString())),
                null,
                null,
                "Based on campaign delivery constraints.");
        }

        if (request.InventoryPackageId is Guid inventoryPackageId)
        {
            var package = await _inventoryPackageRepository.GetByIdAsync(inventoryPackageId);
            if (package is null)
            {
                throw new KeyNotFoundException("Inventory package not found.");
            }

            return new SimulationContext(
                "InventoryPackage",
                null,
                NormalizeSet(package.Cities),
                NormalizeSet(package.BuildingTypes),
                NormalizeSet(package.ScreenOrientations),
                package.ScreenIds.Count > 0 ? package.ScreenIds.ToHashSet() : null,
                package.BuildingIds.Count > 0 ? package.BuildingIds.ToHashSet() : null,
                "Based on inventory package matching.");
        }

        warnings.Add(NoSourceWarning);

        return new SimulationContext(
            "Manual",
            null,
            null,
            null,
            null,
            null,
            null,
            request.ScreenIds is { Count: > 0 }
                ? "Based on manually selected screens."
                : "Based on all active screens.");
    }

    private static List<Screen> FilterScreens(
        IEnumerable<Screen> screens,
        IReadOnlyDictionary<Guid, Building> buildingsById,
        SimulationContext context)
    {
        if (context.ExplicitScreenIds is { Count: > 0 })
        {
            return screens.Where(screen => context.ExplicitScreenIds.Contains(screen.Id)).ToList();
        }

        if (context.ExplicitBuildingIds is { Count: > 0 })
        {
            return screens.Where(screen => context.ExplicitBuildingIds.Contains(screen.BuildingId)).ToList();
        }

        return screens
            .Where(screen =>
            {
                var building = buildingsById[screen.BuildingId];
                return Matches(context.Cities, building.City)
                    && Matches(context.BuildingTypes, building.BuildingType.ToString())
                    && Matches(context.ScreenOrientations, screen.Orientation.ToString());
            })
            .ToList();
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

    private sealed record SimulationContext(
        string Source,
        Guid? CampaignId,
        HashSet<string>? Cities,
        HashSet<string>? BuildingTypes,
        HashSet<string>? ScreenOrientations,
        HashSet<Guid>? ExplicitScreenIds,
        HashSet<Guid>? ExplicitBuildingIds,
        string? ItemNotes);
}
