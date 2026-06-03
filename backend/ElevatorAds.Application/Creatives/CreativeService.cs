using ElevatorAds.Domain.Common;
using ElevatorAds.Application.Creatives.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Creatives;

public sealed class CreativeService
{
    private readonly IAdvertiserRepository _advertiserRepository;
    private readonly ICreativeRepository _creativeRepository;

    public CreativeService(ICreativeRepository creativeRepository, IAdvertiserRepository advertiserRepository)
    {
        _creativeRepository = creativeRepository;
        _advertiserRepository = advertiserRepository;
    }

    public async Task<IReadOnlyList<CreativeDto>> GetAllAsync()
    {
        var creatives = await _creativeRepository.GetAllAsync();
        return creatives.Select(Map).ToList();
    }

    public async Task<PagedResult<CreativeDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _creativeRepository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<CreativeDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<CreativeDto?> GetByIdAsync(Guid id)
    {
        var creative = await _creativeRepository.GetByIdAsync(id);
        return creative is null ? null : Map(creative);
    }

    public async Task<ServiceResult<CreativeDto>> CreateAsync(CreateCreativeRequest request)
    {
        var error = await ValidateAsync(request.AdvertiserId, request.Name, request.MediaUrl, request.DurationSeconds);
        if (error is not null)
        {
            return ServiceResult<CreativeDto>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var creative = new Creative
        {
            Id = Guid.NewGuid(),
            AdvertiserId = request.AdvertiserId,
            Name = request.Name.Trim(),
            MediaUrl = request.MediaUrl.Trim(),
            MediaType = request.MediaType,
            DurationSeconds = request.DurationSeconds,
            ApprovalStatus = ApprovalStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _creativeRepository.AddAsync(creative);
        return ServiceResult<CreativeDto>.Success(Map(created));
    }

    public async Task<ServiceResult<CreativeDto?>> UpdateAsync(Guid id, UpdateCreativeRequest request)
    {
        var error = Validate(request.Name, request.MediaUrl, request.DurationSeconds);
        if (error is not null)
        {
            return ServiceResult<CreativeDto?>.Failure(error);
        }

        var creative = await _creativeRepository.GetByIdAsync(id);
        if (creative is null)
        {
            return ServiceResult<CreativeDto?>.Success(null);
        }

        creative.Name = request.Name.Trim();
        creative.MediaUrl = request.MediaUrl.Trim();
        creative.MediaType = request.MediaType;
        creative.DurationSeconds = request.DurationSeconds;
        creative.UpdatedAt = DateTime.UtcNow;

        var updated = await _creativeRepository.UpdateAsync(creative);
        return ServiceResult<CreativeDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _creativeRepository.DeleteAsync(id);

    public Task<ServiceResult<CreativeDto?>> SubmitForReviewAsync(Guid id) =>
        TransitionAsync(id, ApprovalStatus.Draft, ApprovalStatus.PendingReview, "Creative can only be submitted for review from Draft status.");

    public Task<ServiceResult<CreativeDto?>> ApproveAsync(Guid id) =>
        TransitionAsync(id, ApprovalStatus.PendingReview, ApprovalStatus.Approved, "Creative can only be approved from PendingReview status.");

    public Task<ServiceResult<CreativeDto?>> RejectAsync(Guid id) =>
        TransitionAsync(id, ApprovalStatus.PendingReview, ApprovalStatus.Rejected, "Creative can only be rejected from PendingReview status.");

    private async Task<ServiceResult<CreativeDto?>> TransitionAsync(
        Guid id,
        ApprovalStatus expectedStatus,
        ApprovalStatus targetStatus,
        string invalidTransitionError)
    {
        var creative = await _creativeRepository.GetByIdAsync(id);
        if (creative is null)
        {
            return ServiceResult<CreativeDto?>.Success(null);
        }

        if (creative.ApprovalStatus != expectedStatus)
        {
            return ServiceResult<CreativeDto?>.Failure(invalidTransitionError);
        }

        creative.ApprovalStatus = targetStatus;
        creative.UpdatedAt = DateTime.UtcNow;

        var updated = await _creativeRepository.UpdateAsync(creative);
        return ServiceResult<CreativeDto?>.Success(updated is null ? null : Map(updated));
    }

    private async Task<string?> ValidateAsync(Guid advertiserId, string name, string mediaUrl, int durationSeconds)
    {
        if (advertiserId == Guid.Empty)
        {
            return "AdvertiserId is required.";
        }

        if (await _advertiserRepository.GetByIdAsync(advertiserId) is null)
        {
            return "Advertiser not found.";
        }

        return Validate(name, mediaUrl, durationSeconds);
    }

    private static string? Validate(string name, string mediaUrl, int durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return "MediaUrl is required.";
        }

        if (durationSeconds <= 0)
        {
            return "DurationSeconds must be greater than 0.";
        }

        return null;
    }

    private static CreativeDto Map(Creative creative) =>
        new(
            creative.Id,
            creative.AdvertiserId,
            creative.Name,
            creative.MediaUrl,
            creative.MediaType,
            creative.DurationSeconds,
            creative.ApprovalStatus,
            creative.CreatedAt,
            creative.UpdatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
