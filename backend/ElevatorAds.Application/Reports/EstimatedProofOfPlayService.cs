using ElevatorAds.Application.Reports.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Reports;

public sealed class EstimatedProofOfPlayService
{
    private const string PlaylistFallbackWarning = "Some rows use scheduled playlist data because proof-of-play events were missing.";
    private const string MissingAudienceWarning = "Some buildings are missing estimated daily audience values. Audience and impression estimates may be understated.";
    private const string MissingPlaylistWarning = "Some reported plays had no matching playlist row. Scheduled plays for those rows were aligned to reported plays.";

    private static readonly IReadOnlyList<string> BaseAssumptions =
    [
        "Reported plays come from proof-of-play events when available for the selected campaign and date range.",
        "Scheduled plays come from the latest playlist stored for each screen and day when proof-of-play data is unavailable or incomplete.",
        "Estimated audience is apportioned from each building's EstimatedDailyAudience across the day's effective plays on that screen.",
        "Estimated impressions match estimated audience in this MVP because the platform does not measure rider-level people counts or deduplicated reach."
    ];

    private readonly ICampaignRepository _campaignRepository;
    private readonly IAdvertiserRepository _advertiserRepository;
    private readonly IProofOfPlayEventRepository _proofOfPlayRepository;
    private readonly IDailyPlaylistRepository _playlistRepository;
    private readonly IScreenRepository _screenRepository;
    private readonly ICreativeRepository _creativeRepository;

    public EstimatedProofOfPlayService(
        ICampaignRepository campaignRepository,
        IAdvertiserRepository advertiserRepository,
        IProofOfPlayEventRepository proofOfPlayRepository,
        IDailyPlaylistRepository playlistRepository,
        IScreenRepository screenRepository,
        ICreativeRepository creativeRepository)
    {
        _campaignRepository = campaignRepository;
        _advertiserRepository = advertiserRepository;
        _proofOfPlayRepository = proofOfPlayRepository;
        _playlistRepository = playlistRepository;
        _screenRepository = screenRepository;
        _creativeRepository = creativeRepository;
    }

    public async Task<EstimatedProofOfPlayReportDto?> GenerateAsync(Guid campaignId, DateOnly from, DateOnly to)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId);
        if (campaign is null)
        {
            return null;
        }

        var advertiser = await _advertiserRepository.GetByIdAsync(campaign.AdvertiserId);
        var creativesById = (await _creativeRepository.GetAllAsync()).ToDictionary(item => item.Id);
        var screens = (await _screenRepository.GetAllWithBuildingsAsync()).ToDictionary(item => item.Id);

        var (fromDateTime, toDateTime) = RangeToDateTimes(from, to);
        var reportedEvents = (await _proofOfPlayRepository.GetByDateRangeAsync(fromDateTime, toDateTime))
            .Where(item => item.CampaignId == campaignId)
            .ToList();

        var scheduledCounts = new Dictionary<RowKey, int>();
        foreach (var date in EachDate(from, to))
        {
            foreach (var screen in screens.Values)
            {
                var playlist = await _playlistRepository.GetLatestPublishedByScreenAndDateAsync(screen.Id, date)
                    ?? await _playlistRepository.GetByScreenAndDateAsync(screen.Id, date);

                if (playlist is null)
                {
                    continue;
                }

                foreach (var group in playlist.Items
                             .Where(item => item.CampaignId == campaignId)
                             .GroupBy(item => new RowKey(date, playlist.ScreenId, item.CreativeId)))
                {
                    scheduledCounts[group.Key] = group.Count();
                }
            }
        }

        var reportedCounts = reportedEvents
            .GroupBy(item => new RowKey(DateOnly.FromDateTime(item.PlayedAt), item.ScreenId, item.CreativeId))
            .ToDictionary(group => group.Key, group => group.Count());

        var warnings = new HashSet<string>(StringComparer.Ordinal);

        if (scheduledCounts.Keys.Except(reportedCounts.Keys).Any())
        {
            warnings.Add(PlaylistFallbackWarning);
        }

        var eventOnlyKeys = reportedCounts.Keys.Except(scheduledCounts.Keys).ToList();
        if (eventOnlyKeys.Count > 0)
        {
            warnings.Add(MissingPlaylistWarning);
        }

        var allKeys = scheduledCounts.Keys
            .Union(reportedCounts.Keys)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.ScreenId)
            .ThenBy(item => item.CreativeId)
            .ToList();

        var effectivePlaysByScreenDay = allKeys
            .GroupBy(item => new ScreenDayKey(item.Date, item.ScreenId))
            .ToDictionary(
                group => group.Key,
                group => group.Sum(key =>
                {
                    var scheduled = scheduledCounts.GetValueOrDefault(key);
                    var reported = reportedCounts.GetValueOrDefault(key);
                    return reported > 0 ? reported : scheduled;
                }));

        var items = new List<EstimatedProofOfPlayItemDto>(allKeys.Count);
        foreach (var key in allKeys)
        {
            var screen = screens.GetValueOrDefault(key.ScreenId);
            var building = screen?.Building;
            var scheduledPlays = scheduledCounts.GetValueOrDefault(key);
            var reportedPlays = reportedCounts.GetValueOrDefault(key);

            if (scheduledPlays == 0 && reportedPlays > 0)
            {
                scheduledPlays = reportedPlays;
            }

            var effectivePlays = reportedPlays > 0 ? reportedPlays : scheduledPlays;
            var totalScreenDayPlays = effectivePlaysByScreenDay.GetValueOrDefault(new ScreenDayKey(key.Date, key.ScreenId));
            var estimatedAudience = CalculateEstimatedAudience(building?.EstimatedDailyAudience ?? 0, effectivePlays, totalScreenDayPlays);

            if ((building?.EstimatedDailyAudience ?? 0) <= 0 && effectivePlays > 0)
            {
                warnings.Add(MissingAudienceWarning);
            }

            items.Add(new EstimatedProofOfPlayItemDto(
                key.Date,
                key.ScreenId,
                screen?.Name ?? key.ScreenId.ToString(),
                building?.Id ?? Guid.Empty,
                building?.Name ?? "Unknown building",
                building?.City ?? "Unknown",
                key.CreativeId,
                creativesById.GetValueOrDefault(key.CreativeId)?.Name ?? key.CreativeId.ToString(),
                scheduledPlays,
                reportedPlays,
                estimatedAudience,
                estimatedAudience));
        }

        return new EstimatedProofOfPlayReportDto(
            campaign.Id,
            campaign.Name,
            campaign.AdvertiserId,
            advertiser?.Name ?? campaign.AdvertiserId.ToString(),
            from,
            to,
            items.Sum(item => item.ScheduledPlays),
            items.Sum(item => item.ReportedPlays),
            items.Sum(item => item.EstimatedAudience),
            items.Sum(item => item.EstimatedImpressions),
            items.Select(item => item.ScreenId).Distinct().Count(),
            items.Where(item => item.BuildingId != Guid.Empty).Select(item => item.BuildingId).Distinct().Count(),
            items.Select(item => item.City).Where(item => !string.Equals(item, "Unknown", StringComparison.Ordinal)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item).ToList(),
            items,
            BaseAssumptions,
            warnings.OrderBy(item => item).ToList());
    }

    private static long CalculateEstimatedAudience(int buildingAudience, int effectivePlays, int totalScreenDayPlays)
    {
        if (buildingAudience <= 0 || effectivePlays <= 0 || totalScreenDayPlays <= 0)
        {
            return 0;
        }

        return (long)Math.Round(buildingAudience * (effectivePlays / (double)totalScreenDayPlays), MidpointRounding.AwayFromZero);
    }

    private static IEnumerable<DateOnly> EachDate(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static (DateTime From, DateTime To) RangeToDateTimes(DateOnly from, DateOnly to) =>
        (ToUtcDateTime(from), ToUtcDateTime(to.AddDays(1)));

    private static DateTime ToUtcDateTime(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private readonly record struct RowKey(DateOnly Date, Guid ScreenId, Guid CreativeId);
    private readonly record struct ScreenDayKey(DateOnly Date, Guid ScreenId);
}
