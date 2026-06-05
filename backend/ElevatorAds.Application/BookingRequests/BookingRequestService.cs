using ElevatorAds.Application.BookingRequests.Dtos;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.BookingRequests;

public sealed class BookingRequestService
{
    private static readonly HashSet<string> AllowedBuildingTypes = Enum
        .GetNames<BuildingType>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedScreenOrientations = Enum
        .GetNames<ScreenOrientation>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IAdvertiserRepository _advertiserRepository;
    private readonly ICampaignBookingRequestRepository _repository;

    public BookingRequestService(
        ICampaignBookingRequestRepository repository,
        IAdvertiserRepository advertiserRepository)
    {
        _repository = repository;
        _advertiserRepository = advertiserRepository;
    }

    public async Task<IReadOnlyList<BookingRequestDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(Map).ToList();
    }

    public async Task<PagedResult<BookingRequestDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<BookingRequestDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<BookingRequestDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? null : Map(item);
    }

    public async Task<ServiceResult<BookingRequestDto>> CreateAsync(CreateBookingRequestDto request)
    {
        var error = await ValidateAsync(
            request.AdvertiserId,
            request.Name,
            request.DateFrom,
            request.DateTo,
            request.BuildingTypes,
            request.ScreenOrientations,
            request.CreativeDurationSeconds,
            request.Budget);

        if (error is not null)
        {
            return ServiceResult<BookingRequestDto>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var item = new CampaignBookingRequest
        {
            Id = Guid.NewGuid(),
            AdvertiserId = request.AdvertiserId,
            Name = request.Name.Trim(),
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            Cities = NormalizeList(request.Cities),
            BuildingTypes = NormalizeList(request.BuildingTypes),
            ScreenOrientations = NormalizeList(request.ScreenOrientations),
            CreativeDurationSeconds = request.CreativeDurationSeconds,
            Budget = request.Budget,
            CampaignObjective = request.CampaignObjective?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Status = BookingRequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.AddAsync(item);
        return ServiceResult<BookingRequestDto>.Success(Map(created));
    }

    public async Task<ServiceResult<BookingRequestDto?>> UpdateAsync(Guid id, UpdateBookingRequestDto request)
    {
        var error = Validate(
            request.Name,
            request.DateFrom,
            request.DateTo,
            request.BuildingTypes,
            request.ScreenOrientations,
            request.CreativeDurationSeconds,
            request.Budget);

        if (error is not null)
        {
            return ServiceResult<BookingRequestDto?>.Failure(error);
        }

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return ServiceResult<BookingRequestDto?>.Success(null);
        }

        if (item.Status != BookingRequestStatus.Draft)
        {
            return ServiceResult<BookingRequestDto?>.Failure("Only Draft booking requests can be edited.");
        }

        item.Name = request.Name.Trim();
        item.DateFrom = request.DateFrom;
        item.DateTo = request.DateTo;
        item.Cities = NormalizeList(request.Cities);
        item.BuildingTypes = NormalizeList(request.BuildingTypes);
        item.ScreenOrientations = NormalizeList(request.ScreenOrientations);
        item.CreativeDurationSeconds = request.CreativeDurationSeconds;
        item.Budget = request.Budget;
        item.CampaignObjective = request.CampaignObjective?.Trim() ?? string.Empty;
        item.Notes = request.Notes?.Trim() ?? string.Empty;
        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(item);
        return ServiceResult<BookingRequestDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<ServiceResult<BookingRequestDto?>> SubmitAsync(Guid id) =>
        TransitionAsync(id, BookingRequestStatus.Draft, BookingRequestStatus.Submitted,
            "Booking request can only be submitted from Draft status.");

    public async Task<ServiceResult<BookingRequestDto?>> ApproveAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return ServiceResult<BookingRequestDto?>.Success(null);
        }

        if (item.Status is not (BookingRequestStatus.Submitted or BookingRequestStatus.UnderReview))
        {
            return ServiceResult<BookingRequestDto?>.Failure(
                "Booking request can only be approved from Submitted or UnderReview status.");
        }

        item.Status = BookingRequestStatus.Approved;
        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(item);
        return ServiceResult<BookingRequestDto?>.Success(updated is null ? null : Map(updated));
    }

    public async Task<ServiceResult<BookingRequestDto?>> RejectAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return ServiceResult<BookingRequestDto?>.Success(null);
        }

        if (item.Status is not (BookingRequestStatus.Submitted or BookingRequestStatus.UnderReview))
        {
            return ServiceResult<BookingRequestDto?>.Failure(
                "Booking request can only be rejected from Submitted or UnderReview status.");
        }

        item.Status = BookingRequestStatus.Rejected;
        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(item);
        return ServiceResult<BookingRequestDto?>.Success(updated is null ? null : Map(updated));
    }

    private async Task<ServiceResult<BookingRequestDto?>> TransitionAsync(
        Guid id,
        BookingRequestStatus expectedStatus,
        BookingRequestStatus targetStatus,
        string invalidTransitionError)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return ServiceResult<BookingRequestDto?>.Success(null);
        }

        if (item.Status != expectedStatus)
        {
            return ServiceResult<BookingRequestDto?>.Failure(invalidTransitionError);
        }

        item.Status = targetStatus;
        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(item);
        return ServiceResult<BookingRequestDto?>.Success(updated is null ? null : Map(updated));
    }

    private async Task<string?> ValidateAsync(
        Guid advertiserId,
        string name,
        DateTime dateFrom,
        DateTime dateTo,
        List<string>? buildingTypes,
        List<string>? screenOrientations,
        int creativeDurationSeconds,
        decimal budget)
    {
        if (advertiserId == Guid.Empty)
        {
            return "AdvertiserId is required.";
        }

        if (await _advertiserRepository.GetByIdAsync(advertiserId) is null)
        {
            return "Advertiser not found.";
        }

        return Validate(name, dateFrom, dateTo, buildingTypes, screenOrientations, creativeDurationSeconds, budget);
    }

    private static string? Validate(
        string name,
        DateTime dateFrom,
        DateTime dateTo,
        List<string>? buildingTypes,
        List<string>? screenOrientations,
        int creativeDurationSeconds,
        decimal budget)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (dateFrom > dateTo)
        {
            return "DateFrom must be before or equal to DateTo.";
        }

        if (creativeDurationSeconds <= 0)
        {
            return "CreativeDurationSeconds must be greater than 0.";
        }

        if (budget < 0)
        {
            return "Budget cannot be negative.";
        }

        var invalidBuildingType = NormalizeList(buildingTypes)
            .FirstOrDefault(value => !AllowedBuildingTypes.Contains(value));
        if (invalidBuildingType is not null)
        {
            return $"Invalid BuildingType '{invalidBuildingType}'.";
        }

        var invalidOrientation = NormalizeList(screenOrientations)
            .FirstOrDefault(value => !AllowedScreenOrientations.Contains(value));
        if (invalidOrientation is not null)
        {
            return $"Invalid ScreenOrientation '{invalidOrientation}'.";
        }

        return null;
    }

    private static List<string> NormalizeList(List<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? [];

    private static BookingRequestDto Map(CampaignBookingRequest item) =>
        new(
            item.Id,
            item.AdvertiserId,
            item.Name,
            item.DateFrom,
            item.DateTo,
            item.Cities,
            item.BuildingTypes,
            item.ScreenOrientations,
            item.CreativeDurationSeconds,
            item.Budget,
            item.CampaignObjective,
            item.Notes,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);
        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
