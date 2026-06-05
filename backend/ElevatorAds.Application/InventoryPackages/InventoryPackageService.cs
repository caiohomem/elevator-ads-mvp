using ElevatorAds.Application.InventoryPackages.Dtos;
using ElevatorAds.Application.Screens.Dtos;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.InventoryPackages;

public sealed class InventoryPackageService
{
    private static readonly HashSet<string> AllowedBuildingTypes = Enum
        .GetNames<BuildingType>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedScreenOrientations = Enum
        .GetNames<ScreenOrientation>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IInventoryPackageRepository _repository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IScreenRepository _screenRepository;

    public InventoryPackageService(
        IInventoryPackageRepository repository,
        IBuildingRepository buildingRepository,
        IScreenRepository screenRepository)
    {
        _repository = repository;
        _buildingRepository = buildingRepository;
        _screenRepository = screenRepository;
    }

    public async Task<PagedResult<InventoryPackageDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<InventoryPackageDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<InventoryPackageDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? null : Map(item);
    }

    public async Task<ServiceResult<InventoryPackageDto>> CreateAsync(CreateInventoryPackageRequest request)
    {
        var validation = await ValidateAsync(
            request.Name,
            request.BuildingTypes,
            request.ScreenOrientations,
            request.ScreenIds,
            request.BuildingIds,
            request.BaseCpm);

        if (validation is not null)
        {
            return ServiceResult<InventoryPackageDto>.Failure(validation);
        }

        var now = DateTime.UtcNow;
        var item = new InventoryPackage
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Cities = NormalizeStringList(request.Cities),
            BuildingTypes = NormalizeStringList(request.BuildingTypes),
            ScreenOrientations = NormalizeStringList(request.ScreenOrientations),
            ScreenIds = NormalizeGuidList(request.ScreenIds),
            BuildingIds = NormalizeGuidList(request.BuildingIds),
            BaseCpm = request.BaseCpm,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.AddAsync(item);
        return ServiceResult<InventoryPackageDto>.Success(Map(created));
    }

    public async Task<ServiceResult<InventoryPackageDto?>> UpdateAsync(Guid id, UpdateInventoryPackageRequest request)
    {
        var validation = await ValidateAsync(
            request.Name,
            request.BuildingTypes,
            request.ScreenOrientations,
            request.ScreenIds,
            request.BuildingIds,
            request.BaseCpm);

        if (validation is not null)
        {
            return ServiceResult<InventoryPackageDto?>.Failure(validation);
        }

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return ServiceResult<InventoryPackageDto?>.Success(null);
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.Cities = NormalizeStringList(request.Cities);
        item.BuildingTypes = NormalizeStringList(request.BuildingTypes);
        item.ScreenOrientations = NormalizeStringList(request.ScreenOrientations);
        item.ScreenIds = NormalizeGuidList(request.ScreenIds);
        item.BuildingIds = NormalizeGuidList(request.BuildingIds);
        item.BaseCpm = request.BaseCpm;
        item.Status = request.Status;
        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(item);
        return ServiceResult<InventoryPackageDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

    public async Task<IReadOnlyList<ScreenDto>?> GetMatchingScreensAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return null;
        }

        var normalizedCities = NormalizeStringList(item.Cities).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedBuildingTypes = NormalizeStringList(item.BuildingTypes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedScreenOrientations = NormalizeStringList(item.ScreenOrientations).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buildingIds = NormalizeGuidList(item.BuildingIds).ToHashSet();
        var screenIds = NormalizeGuidList(item.ScreenIds).ToHashSet();

        var screens = await _screenRepository.GetAllWithBuildingsAsync();

        return screens
            .Where(screen => Matches(screen, normalizedCities, normalizedBuildingTypes, normalizedScreenOrientations, buildingIds, screenIds))
            .Select(MapScreen)
            .ToList();
    }

    private async Task<string?> ValidateAsync(
        string name,
        List<string>? buildingTypes,
        List<string>? screenOrientations,
        List<Guid>? screenIds,
        List<Guid>? buildingIds,
        decimal baseCpm)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (baseCpm < 0)
        {
            return "BaseCpm cannot be negative.";
        }

        var invalidBuildingType = NormalizeStringList(buildingTypes)
            .FirstOrDefault(value => !AllowedBuildingTypes.Contains(value));
        if (invalidBuildingType is not null)
        {
            return $"Invalid BuildingType '{invalidBuildingType}'.";
        }

        var invalidScreenOrientation = NormalizeStringList(screenOrientations)
            .FirstOrDefault(value => !AllowedScreenOrientations.Contains(value));
        if (invalidScreenOrientation is not null)
        {
            return $"Invalid ScreenOrientation '{invalidScreenOrientation}'.";
        }

        foreach (var buildingId in NormalizeGuidList(buildingIds))
        {
            if (await _buildingRepository.GetByIdAsync(buildingId) is null)
            {
                return $"BuildingId '{buildingId}' was not found.";
            }
        }

        foreach (var screenId in NormalizeGuidList(screenIds))
        {
            if (await _screenRepository.GetByIdAsync(screenId) is null)
            {
                return $"ScreenId '{screenId}' was not found.";
            }
        }

        return null;
    }

    private static bool Matches(
        Screen screen,
        HashSet<string> cities,
        HashSet<string> buildingTypes,
        HashSet<string> screenOrientations,
        HashSet<Guid> buildingIds,
        HashSet<Guid> screenIds)
    {
        if (screenIds.Contains(screen.Id))
        {
            return true;
        }

        if (buildingIds.Contains(screen.BuildingId))
        {
            return true;
        }

        var matchesCity = cities.Count == 0
            || (screen.Building?.City is not null && cities.Contains(screen.Building.City.Trim()));

        var matchesBuildingType = buildingTypes.Count == 0
            || (screen.Building is not null && buildingTypes.Contains(screen.Building.BuildingType.ToString()));

        var matchesOrientation = screenOrientations.Count == 0
            || screenOrientations.Contains(screen.Orientation.ToString());

        return matchesCity && matchesBuildingType && matchesOrientation;
    }

    private static InventoryPackageDto Map(InventoryPackage item) =>
        new(
            item.Id,
            item.Name,
            item.Description,
            item.Cities.ToList(),
            item.BuildingTypes.ToList(),
            item.ScreenOrientations.ToList(),
            item.ScreenIds.ToList(),
            item.BuildingIds.ToList(),
            item.BaseCpm,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt);

    private static ScreenDto MapScreen(Screen screen) =>
        new(
            screen.Id,
            screen.BuildingId,
            screen.Name,
            screen.ExternalCode,
            screen.ResolutionWidth,
            screen.ResolutionHeight,
            screen.Orientation,
            screen.Status,
            screen.LastSeenAt,
            screen.CreatedAt,
            screen.UpdatedAt);

    private static List<string> NormalizeStringList(List<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? [];

    private static List<Guid> NormalizeGuidList(List<Guid>? values) =>
        values?
            .Where(value => value != Guid.Empty)
            .Distinct()
            .ToList()
        ?? [];

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);
        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
