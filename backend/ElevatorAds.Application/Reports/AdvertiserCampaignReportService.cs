using ElevatorAds.Application.Reports.Dtos;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Reports;

public sealed class AdvertiserCampaignReportService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICreativeRepository _creativeRepository;
    private readonly EstimatedProofOfPlayService _estimatedProofOfPlayService;

    public AdvertiserCampaignReportService(
        ICampaignRepository campaignRepository,
        ICreativeRepository creativeRepository,
        EstimatedProofOfPlayService estimatedProofOfPlayService)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _estimatedProofOfPlayService = estimatedProofOfPlayService;
    }

    public async Task<AdvertiserCampaignReportDto?> GenerateAsync(Guid advertiserId, Guid campaignId, DateOnly from, DateOnly to)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId);
        if (campaign is null || campaign.AdvertiserId != advertiserId)
        {
            return null;
        }

        var creativesById = (await _creativeRepository.GetAllAsync()).ToDictionary(item => item.Id);
        var baseReport = await _estimatedProofOfPlayService.GenerateAsync(campaignId, from, to);
        if (baseReport is null)
        {
            return null;
        }

        var creatives = baseReport.Items
            .GroupBy(item => item.CreativeId)
            .OrderBy(group => creativesById.GetValueOrDefault(group.Key)?.Name ?? group.First().CreativeName)
            .Select(group =>
            {
                var creative = creativesById.GetValueOrDefault(group.Key);
                return new AdvertiserCampaignCreativeSummaryDto(
                    group.Key,
                    creative?.Name ?? group.First().CreativeName,
                    creative?.MediaType.ToString() ?? "Unknown",
                    creative?.DurationSeconds ?? 0,
                    group.Sum(GetEffectivePlays),
                    group.Sum(item => item.EstimatedImpressions));
            })
            .ToList();

        var dailyBreakdown = baseReport.Items
            .GroupBy(item => item.Date)
            .OrderBy(group => group.Key)
            .Select(group => new AdvertiserCampaignDailyBreakdownDto(
                group.Key,
                group.Sum(GetEffectivePlays),
                group.Sum(item => item.EstimatedAudience),
                group.Sum(item => item.EstimatedImpressions),
                group.Select(item => item.ScreenId).Distinct().Count(),
                group.Where(item => item.BuildingId != Guid.Empty).Select(item => item.BuildingId).Distinct().Count()))
            .ToList();

        return new AdvertiserCampaignReportDto(
            baseReport.AdvertiserId,
            baseReport.AdvertiserName,
            baseReport.CampaignId,
            baseReport.CampaignName,
            baseReport.DateFrom,
            baseReport.DateTo,
            campaign.Status.ToString(),
            baseReport.Items.Sum(GetEffectivePlays),
            baseReport.TotalScheduledPlays,
            baseReport.TotalReportedPlays,
            baseReport.EstimatedAudience,
            baseReport.EstimatedImpressions,
            baseReport.ScreensCount,
            baseReport.BuildingsCount,
            baseReport.Cities,
            creatives,
            dailyBreakdown,
            baseReport.Assumptions,
            baseReport.Warnings);
    }

    private static int GetEffectivePlays(EstimatedProofOfPlayItemDto item) =>
        item.ReportedPlays > 0 ? item.ReportedPlays : item.ScheduledPlays;
}
