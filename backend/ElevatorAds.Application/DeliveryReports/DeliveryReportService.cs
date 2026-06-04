using ElevatorAds.Application.DeliveryReports.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.DeliveryReports;

public sealed class DeliveryReportService
{
    private readonly IProofOfPlayEventRepository _proofOfPlayRepository;

    public DeliveryReportService(IProofOfPlayEventRepository proofOfPlayRepository)
    {
        _proofOfPlayRepository = proofOfPlayRepository;
    }

    public async Task<OverviewReportDto> GetOverviewAsync(DateOnly date)
    {
        var from = ToUtcDateTime(date);
        var to = ToUtcDateTime(date.AddDays(1));
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(from, to)).ToList();

        return new OverviewReportDto(
            date,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.CampaignId),
            GroupBy(events, item => item.ScreenId),
            GroupBy(events, item => item.CreativeId));
    }

    public async Task<CampaignReportDto> GetCampaignsAsync(DateOnly from, DateOnly to)
    {
        var (fromDateTime, toDateTime) = RangeToDateTimes(from, to);
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(fromDateTime, toDateTime)).ToList();

        return new CampaignReportDto(
            from,
            to,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.CampaignId));
    }

    public async Task<ScreenReportDto> GetScreensAsync(DateOnly from, DateOnly to)
    {
        var (fromDateTime, toDateTime) = RangeToDateTimes(from, to);
        var events = (await _proofOfPlayRepository.GetByDateRangeAsync(fromDateTime, toDateTime)).ToList();

        return new ScreenReportDto(
            from,
            to,
            events.Count,
            events.Sum(item => (long)item.DurationSeconds),
            GroupBy(events, item => item.ScreenId));
    }

    private static (DateTime From, DateTime To) RangeToDateTimes(DateOnly from, DateOnly to) =>
        (ToUtcDateTime(from), ToUtcDateTime(to.AddDays(1)));

    private static DateTime ToUtcDateTime(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static IReadOnlyList<GroupSummaryDto> GroupBy(
        IEnumerable<ProofOfPlayEvent> events,
        Func<ProofOfPlayEvent, Guid> keySelector) =>
        events
            .GroupBy(keySelector)
            .Select(group => new GroupSummaryDto(
                group.Key,
                group.Count(),
                group.Sum(item => (long)item.DurationSeconds)))
            .OrderByDescending(summary => summary.Plays)
            .ThenBy(summary => summary.Id)
            .ToList();
}
