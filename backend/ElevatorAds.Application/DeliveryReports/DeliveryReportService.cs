using ElevatorAds.Application.DeliveryReports.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.DeliveryReports;

public sealed class DeliveryReportService
{
    private readonly IProofOfPlayEventRepository _proofOfPlayRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IScreenRepository _screenRepository;
    private readonly ICreativeRepository _creativeRepository;

    public DeliveryReportService(
        IProofOfPlayEventRepository proofOfPlayRepository,
        ICampaignRepository campaignRepository,
        IScreenRepository screenRepository,
        ICreativeRepository creativeRepository)
    {
        _proofOfPlayRepository = proofOfPlayRepository;
        _campaignRepository = campaignRepository;
        _screenRepository = screenRepository;
        _creativeRepository = creativeRepository;
    }

    public async Task<OverviewReportDto> GetOverviewAsync(DateOnly date)
    {
        var from = ToUtcDateTime(date);
        var to = ToUtcDateTime(date.AddDays(1));
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(from, to)).ToList();
        var campaignNames = await BuildCampaignNamesAsync(events.Select(item => item.CampaignId));
        var screenNames = await BuildScreenNamesAsync(events.Select(item => item.ScreenId));
        var creativeNames = await BuildCreativeNamesAsync(events.Select(item => item.CreativeId));

        return new OverviewReportDto(
            date,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.CampaignId, campaignNames),
            GroupBy(events, item => item.ScreenId, screenNames),
            GroupBy(events, item => item.CreativeId, creativeNames));
    }

    public async Task<CampaignReportDto> GetCampaignsAsync(DateOnly from, DateOnly to)
    {
        var (fromDateTime, toDateTime) = RangeToDateTimes(from, to);
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(fromDateTime, toDateTime)).ToList();
        var campaignNames = await BuildCampaignNamesAsync(events.Select(item => item.CampaignId));

        return new CampaignReportDto(
            from,
            to,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.CampaignId, campaignNames));
    }

    public async Task<ScreenReportDto> GetScreensAsync(DateOnly from, DateOnly to)
    {
        var (fromDateTime, toDateTime) = RangeToDateTimes(from, to);
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(fromDateTime, toDateTime)).ToList();
        var screenNames = await BuildScreenNamesAsync(events.Select(item => item.ScreenId));

        return new ScreenReportDto(
            from,
            to,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.ScreenId, screenNames));
    }

    private static (DateTime From, DateTime To) RangeToDateTimes(DateOnly from, DateOnly to) =>
        (ToUtcDateTime(from), ToUtcDateTime(to.AddDays(1)));

    private static DateTime ToUtcDateTime(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static IReadOnlyList<GroupSummaryDto> GroupBy(
        IEnumerable<ProofOfPlayEvent> events,
        Func<ProofOfPlayEvent, Guid> keySelector,
        IReadOnlyDictionary<Guid, string> namesById) =>
        events
            .GroupBy(keySelector)
            .Select(group => new GroupSummaryDto(
                group.Key,
                namesById.GetValueOrDefault(group.Key, group.Key.ToString()),
                group.Count(),
                group.Sum(item => (long)item.DurationSeconds)))
            .OrderByDescending(summary => summary.Plays)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<IReadOnlyDictionary<Guid, string>> BuildCampaignNamesAsync(IEnumerable<Guid> campaignIds)
    {
        var ids = campaignIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _campaignRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => item.Name);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildScreenNamesAsync(IEnumerable<Guid> screenIds)
    {
        var ids = screenIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _screenRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => string.IsNullOrWhiteSpace(item.Name) ? item.ExternalCode : item.Name);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildCreativeNamesAsync(IEnumerable<Guid> creativeIds)
    {
        var ids = creativeIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _creativeRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => item.Name);
    }
}
