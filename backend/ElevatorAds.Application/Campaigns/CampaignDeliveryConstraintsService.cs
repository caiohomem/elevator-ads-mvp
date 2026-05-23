using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Campaigns;

public sealed class CampaignDeliveryConstraintsService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignDeliveryConstraintsRepository _constraintsRepository;

    public CampaignDeliveryConstraintsService(
        ICampaignRepository campaignRepository,
        ICampaignDeliveryConstraintsRepository constraintsRepository)
    {
        _campaignRepository = campaignRepository;
        _constraintsRepository = constraintsRepository;
    }

    public async Task<DeliveryConstraintsDto?> GetByCampaignIdAsync(Guid campaignId)
    {
        if (await _campaignRepository.GetByIdAsync(campaignId) is null)
        {
            return null;
        }

        var constraints = await _constraintsRepository.GetByCampaignIdAsync(campaignId);
        return constraints is null ? null : Map(constraints);
    }

    public async Task<ServiceResult<DeliveryConstraintsDto>> UpsertAsync(
        Guid campaignId,
        UpsertDeliveryConstraintsRequest request)
    {
        if (await _campaignRepository.GetByIdAsync(campaignId) is null)
        {
            return ServiceResult<DeliveryConstraintsDto>.Failure("Campaign not found.");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime.Value >= request.EndTime.Value)
        {
            return ServiceResult<DeliveryConstraintsDto>.Failure("StartTime must be before EndTime.");
        }

        var buildingTypes = ParseEnums<BuildingType>(request.BuildingTypes, "BuildingType");
        if (!buildingTypes.IsSuccess)
        {
            return ServiceResult<DeliveryConstraintsDto>.Failure(buildingTypes.Error!);
        }

        var screenOrientations = ParseEnums<ScreenOrientation>(request.ScreenOrientations, "ScreenOrientation");
        if (!screenOrientations.IsSuccess)
        {
            return ServiceResult<DeliveryConstraintsDto>.Failure(screenOrientations.Error!);
        }

        var daysOfWeek = ParseDaysOfWeek(request.DaysOfWeek);
        if (!daysOfWeek.IsSuccess)
        {
            return ServiceResult<DeliveryConstraintsDto>.Failure(daysOfWeek.Error!);
        }

        var existing = await _constraintsRepository.GetByCampaignIdAsync(campaignId);
        var constraints = new CampaignDeliveryConstraints
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            CampaignId = campaignId,
            Cities = request.Cities
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Select(city => city.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            BuildingTypes = buildingTypes.Value!,
            ScreenOrientations = screenOrientations.Value!,
            DaysOfWeek = daysOfWeek.Value!,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var saved = await _constraintsRepository.UpsertAsync(constraints);
        return ServiceResult<DeliveryConstraintsDto>.Success(Map(saved));
    }

    private static ServiceResult<List<TEnum>> ParseEnums<TEnum>(IReadOnlyList<string> values, string fieldName)
        where TEnum : struct, Enum
    {
        var parsedValues = new List<TEnum>();

        foreach (var value in values)
        {
            if (!Enum.TryParse<TEnum>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return ServiceResult<List<TEnum>>.Failure($"Invalid {fieldName} value: {value}.");
            }

            if (!parsedValues.Contains(parsed))
            {
                parsedValues.Add(parsed);
            }
        }

        return ServiceResult<List<TEnum>>.Success(parsedValues);
    }

    private static ServiceResult<List<DayOfWeek>> ParseDaysOfWeek(IReadOnlyList<string> values)
    {
        var parsedValues = new List<DayOfWeek>();

        foreach (var value in values)
        {
            if (!Enum.TryParse<DayOfWeek>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return ServiceResult<List<DayOfWeek>>.Failure($"Invalid DaysOfWeek value: {value}.");
            }

            if (!parsedValues.Contains(parsed))
            {
                parsedValues.Add(parsed);
            }
        }

        return ServiceResult<List<DayOfWeek>>.Success(parsedValues);
    }

    private static DeliveryConstraintsDto Map(CampaignDeliveryConstraints constraints) =>
        new(
            constraints.Id,
            constraints.CampaignId,
            constraints.Cities,
            constraints.BuildingTypes,
            constraints.ScreenOrientations,
            constraints.DaysOfWeek,
            constraints.StartTime,
            constraints.EndTime,
            constraints.CreatedAt,
            constraints.UpdatedAt);

    public sealed record DeliveryConstraintsDto(
        Guid Id,
        Guid CampaignId,
        IReadOnlyList<string> Cities,
        IReadOnlyList<BuildingType> BuildingTypes,
        IReadOnlyList<ScreenOrientation> ScreenOrientations,
        IReadOnlyList<DayOfWeek> DaysOfWeek,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record UpsertDeliveryConstraintsRequest(
        IReadOnlyList<string> Cities,
        IReadOnlyList<string> BuildingTypes,
        IReadOnlyList<string> ScreenOrientations,
        IReadOnlyList<string> DaysOfWeek,
        TimeOnly? StartTime,
        TimeOnly? EndTime);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
