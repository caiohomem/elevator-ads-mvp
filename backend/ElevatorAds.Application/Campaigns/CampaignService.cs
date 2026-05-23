using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Campaigns;

public sealed class CampaignService
{
    private readonly IAdvertiserRepository _advertiserRepository;
    private readonly ICampaignRepository _campaignRepository;

    public CampaignService(ICampaignRepository campaignRepository, IAdvertiserRepository advertiserRepository)
    {
        _campaignRepository = campaignRepository;
        _advertiserRepository = advertiserRepository;
    }

    public async Task<IReadOnlyList<CampaignDto>> GetAllAsync()
    {
        var campaigns = await _campaignRepository.GetAllAsync();
        return campaigns.Select(Map).ToList();
    }

    public async Task<CampaignDto?> GetByIdAsync(Guid id)
    {
        var campaign = await _campaignRepository.GetByIdAsync(id);
        return campaign is null ? null : Map(campaign);
    }

    public async Task<ServiceResult<CampaignDto>> CreateAsync(CreateCampaignRequest request)
    {
        var error = await ValidateCreateAsync(request);
        if (error is not null)
        {
            return ServiceResult<CampaignDto>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            AdvertiserId = request.AdvertiserId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            DailyBudget = request.DailyBudget,
            TotalBudget = request.TotalBudget,
            MaxCpm = request.MaxCpm,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _campaignRepository.AddAsync(campaign);
        return ServiceResult<CampaignDto>.Success(Map(created));
    }

    public async Task<ServiceResult<CampaignDto?>> UpdateAsync(Guid id, UpdateCampaignRequest request)
    {
        var error = Validate(request.Name, request.StartDate, request.EndDate, request.DailyBudget, request.TotalBudget, request.MaxCpm);
        if (error is not null)
        {
            return ServiceResult<CampaignDto?>.Failure(error);
        }

        var campaign = await _campaignRepository.GetByIdAsync(id);
        if (campaign is null)
        {
            return ServiceResult<CampaignDto?>.Success(null);
        }

        campaign.Name = request.Name.Trim();
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;
        campaign.Status = request.Status;
        campaign.DailyBudget = request.DailyBudget;
        campaign.TotalBudget = request.TotalBudget;
        campaign.MaxCpm = request.MaxCpm;

        var updated = await _campaignRepository.UpdateAsync(campaign);
        return ServiceResult<CampaignDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _campaignRepository.DeleteAsync(id);

    private async Task<string?> ValidateCreateAsync(CreateCampaignRequest request)
    {
        if (request.AdvertiserId == Guid.Empty)
        {
            return "AdvertiserId is required.";
        }

        if (await _advertiserRepository.GetByIdAsync(request.AdvertiserId) is null)
        {
            return "Advertiser not found.";
        }

        return Validate(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.DailyBudget,
            request.TotalBudget,
            request.MaxCpm);
    }

    private static string? Validate(
        string name,
        DateTime? startDate,
        DateTime? endDate,
        decimal? dailyBudget,
        decimal? totalBudget,
        decimal? maxCpm)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return "StartDate must be before or equal to EndDate.";
        }

        if (dailyBudget < 0)
        {
            return "DailyBudget cannot be negative.";
        }

        if (totalBudget < 0)
        {
            return "TotalBudget cannot be negative.";
        }

        if (maxCpm < 0)
        {
            return "MaxCpm cannot be negative.";
        }

        return null;
    }

    private static CampaignDto Map(Campaign campaign) =>
        new(
            campaign.Id,
            campaign.AdvertiserId,
            campaign.Name,
            campaign.StartDate,
            campaign.EndDate,
            campaign.Status,
            campaign.DailyBudget,
            campaign.TotalBudget,
            campaign.MaxCpm,
            campaign.CreatedAt,
            campaign.UpdatedAt);

    public sealed record CampaignDto(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        CampaignStatus Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record CreateCampaignRequest(
        Guid AdvertiserId,
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        CampaignStatus Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm);

    public sealed record UpdateCampaignRequest(
        string Name,
        DateTime? StartDate,
        DateTime? EndDate,
        CampaignStatus Status,
        decimal? DailyBudget,
        decimal? TotalBudget,
        decimal? MaxCpm);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
