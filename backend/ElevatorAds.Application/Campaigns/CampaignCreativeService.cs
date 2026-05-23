using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Campaigns;

public sealed class CampaignCreativeService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICreativeRepository _creativeRepository;
    private readonly ICampaignCreativeRepository _campaignCreativeRepository;

    public CampaignCreativeService(
        ICampaignRepository campaignRepository,
        ICreativeRepository creativeRepository,
        ICampaignCreativeRepository campaignCreativeRepository)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _campaignCreativeRepository = campaignCreativeRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<CampaignCreativeDto>>> GetByCampaignIdAsync(Guid campaignId)
    {
        if (await _campaignRepository.GetByIdAsync(campaignId) is null)
        {
            return ServiceResult<IReadOnlyList<CampaignCreativeDto>>.Failure("Campaign not found.");
        }

        var assignments = await _campaignCreativeRepository.GetByCampaignIdAsync(campaignId);
        return ServiceResult<IReadOnlyList<CampaignCreativeDto>>.Success(assignments.Select(Map).ToList());
    }

    public async Task<ServiceResult<CampaignCreativeDto>> AssignAsync(Guid campaignId, Guid creativeId)
    {
        if (await _campaignRepository.GetByIdAsync(campaignId) is null)
        {
            return ServiceResult<CampaignCreativeDto>.Failure("Campaign not found.");
        }

        var creative = await _creativeRepository.GetByIdAsync(creativeId);
        if (creative is null)
        {
            return ServiceResult<CampaignCreativeDto>.Failure("Creative not found.");
        }

        if (creative.ApprovalStatus != ApprovalStatus.Approved)
        {
            return ServiceResult<CampaignCreativeDto>.Failure("Creative must be Approved before assignment.");
        }

        if (await _campaignCreativeRepository.GetAsync(campaignId, creativeId) is not null)
        {
            return ServiceResult<CampaignCreativeDto>.Failure("Creative is already assigned to campaign.");
        }

        var assignment = new CampaignCreative
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            CreativeId = creativeId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _campaignCreativeRepository.AddAsync(assignment);
        return ServiceResult<CampaignCreativeDto>.Success(Map(created));
    }

    public Task<bool> RemoveAsync(Guid campaignId, Guid creativeId) =>
        _campaignCreativeRepository.DeleteAsync(campaignId, creativeId);

    private static CampaignCreativeDto Map(CampaignCreative assignment) =>
        new(assignment.Id, assignment.CampaignId, assignment.CreativeId, assignment.CreatedAt);

    public sealed record CampaignCreativeDto(Guid Id, Guid CampaignId, Guid CreativeId, DateTime CreatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
