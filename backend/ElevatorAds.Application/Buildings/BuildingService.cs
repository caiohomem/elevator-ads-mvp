using ElevatorAds.Domain.Common;
using ElevatorAds.Application.Buildings.Dtos;
using ElevatorAds.Application.Organizations;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Buildings;

public sealed class BuildingService
{
    private const string DefaultOrganizationName = "Default Organization";
    private const string DefaultOrganizationSlug = "default";

    private readonly IBuildingRepository _repository;
    private readonly OrganizationService _organizationService;

    public BuildingService(IBuildingRepository repository, OrganizationService organizationService)
    {
        _repository = repository;
        _organizationService = organizationService;
    }

    public async Task<IReadOnlyList<BuildingDto>> GetAllAsync()
    {
        var buildings = await _repository.GetAllAsync();
        return buildings.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<BuildingDto>> GetByOrganizationAsync(Guid organizationId)
    {
        var buildings = await _repository.GetByOrganizationAsync(organizationId);
        return buildings.Select(Map).ToList();
    }

    public async Task<PagedResult<BuildingDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<BuildingDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<BuildingDto?> GetByIdAsync(Guid id)
    {
        var building = await _repository.GetByIdAsync(id);
        return building is null ? null : Map(building);
    }

    public async Task<ServiceResult<BuildingDto>> CreateAsync(CreateBuildingRequest request)
    {
        var error = Validate(request.Name, request.City, request.Country, request.EstimatedDailyAudience);
        if (error is not null)
        {
            return ServiceResult<BuildingDto>.Failure(error);
        }

        var organizationId = await _organizationService.EnsureDefaultOrganizationIdAsync(DefaultOrganizationName, DefaultOrganizationSlug);

        var now = DateTime.UtcNow;
        var building = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            PostalCode = request.PostalCode.Trim(),
            BuildingType = request.BuildingType,
            EstimatedDailyAudience = request.EstimatedDailyAudience,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.AddAsync(building);
        return ServiceResult<BuildingDto>.Success(Map(created));
    }

    public async Task<ServiceResult<BuildingDto?>> UpdateAsync(Guid id, UpdateBuildingRequest request)
    {
        var error = Validate(request.Name, request.City, request.Country, request.EstimatedDailyAudience);
        if (error is not null)
        {
            return ServiceResult<BuildingDto?>.Failure(error);
        }

        var building = await _repository.GetByIdAsync(id);
        if (building is null)
        {
            return ServiceResult<BuildingDto?>.Success(null);
        }

        building.Name = request.Name.Trim();
        building.Address = request.Address.Trim();
        building.City = request.City.Trim();
        building.Country = request.Country.Trim();
        building.PostalCode = request.PostalCode.Trim();
        building.BuildingType = request.BuildingType;
        building.EstimatedDailyAudience = request.EstimatedDailyAudience;
        building.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(building);
        return ServiceResult<BuildingDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

    private static string? Validate(string name, string city, string country, int estimatedDailyAudience)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return "City is required.";
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return "Country is required.";
        }

        if (estimatedDailyAudience < 0)
        {
            return "EstimatedDailyAudience cannot be negative.";
        }

        return null;
    }

    private static BuildingDto Map(Building building) =>
        new(
            building.Id,
            building.Name,
            building.Address,
            building.City,
            building.Country,
            building.PostalCode,
            building.BuildingType,
            building.EstimatedDailyAudience,
            building.CreatedAt,
            building.UpdatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);
        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
