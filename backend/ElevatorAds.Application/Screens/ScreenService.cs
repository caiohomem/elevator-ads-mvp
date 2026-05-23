using ElevatorAds.Application.Screens.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Screens;

public sealed class ScreenService
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly IScreenRepository _screenRepository;

    public ScreenService(IScreenRepository screenRepository, IBuildingRepository buildingRepository)
    {
        _screenRepository = screenRepository;
        _buildingRepository = buildingRepository;
    }

    public async Task<IReadOnlyList<ScreenDto>> GetAllAsync()
    {
        var screens = await _screenRepository.GetAllAsync();
        return screens.Select(Map).ToList();
    }

    public async Task<ScreenDto?> GetByIdAsync(Guid id)
    {
        var screen = await _screenRepository.GetByIdAsync(id);
        return screen is null ? null : Map(screen);
    }

    public async Task<ServiceResult<ScreenDto>> CreateAsync(CreateScreenRequest request)
    {
        var error = await ValidateAsync(request.BuildingId, request.Name, request.ResolutionWidth, request.ResolutionHeight);
        if (error is not null)
        {
            return ServiceResult<ScreenDto>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var screen = new Screen
        {
            Id = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            Name = request.Name.Trim(),
            ExternalCode = request.ExternalCode.Trim(),
            ResolutionWidth = request.ResolutionWidth,
            ResolutionHeight = request.ResolutionHeight,
            Orientation = request.Orientation,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _screenRepository.AddAsync(screen);
        return ServiceResult<ScreenDto>.Success(Map(created));
    }

    public async Task<ServiceResult<ScreenDto?>> UpdateAsync(Guid id, UpdateScreenRequest request)
    {
        var error = await ValidateAsync(request.BuildingId, request.Name, request.ResolutionWidth, request.ResolutionHeight);
        if (error is not null)
        {
            return ServiceResult<ScreenDto?>.Failure(error);
        }

        var screen = await _screenRepository.GetByIdAsync(id);
        if (screen is null)
        {
            return ServiceResult<ScreenDto?>.Success(null);
        }

        screen.BuildingId = request.BuildingId;
        screen.Name = request.Name.Trim();
        screen.ExternalCode = request.ExternalCode.Trim();
        screen.ResolutionWidth = request.ResolutionWidth;
        screen.ResolutionHeight = request.ResolutionHeight;
        screen.Orientation = request.Orientation;
        screen.Status = request.Status;
        screen.UpdatedAt = DateTime.UtcNow;

        var updated = await _screenRepository.UpdateAsync(screen);
        return ServiceResult<ScreenDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _screenRepository.DeleteAsync(id);

    public async Task<ScreenDto?> StatusCheckAsync(Guid id)
    {
        var updated = await _screenRepository.UpdateLastSeenAtAsync(id, DateTime.UtcNow);
        return updated is null ? null : Map(updated);
    }

    private async Task<string?> ValidateAsync(Guid buildingId, string name, int resolutionWidth, int resolutionHeight)
    {
        if (buildingId == Guid.Empty)
        {
            return "BuildingId is required.";
        }

        if (await _buildingRepository.GetByIdAsync(buildingId) is null)
        {
            return "Building not found.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (resolutionWidth <= 0)
        {
            return "ResolutionWidth must be greater than 0.";
        }

        if (resolutionHeight <= 0)
        {
            return "ResolutionHeight must be greater than 0.";
        }

        return null;
    }

    private static ScreenDto Map(Screen screen) =>
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

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
