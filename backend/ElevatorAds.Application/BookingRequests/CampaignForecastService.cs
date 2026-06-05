using ElevatorAds.Application.BookingRequests.Dtos;
using ElevatorAds.Application.Programmatic;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.BookingRequests;

public sealed class CampaignForecastService
{
    private readonly ICampaignBookingRequestRepository _bookingRequestRepository;
    private readonly ICampaignForecastRepository _forecastRepository;
    private readonly SimulatorForecastService _simulatorForecastService;
    private readonly TimeProvider _timeProvider;

    public CampaignForecastService(
        ICampaignBookingRequestRepository bookingRequestRepository,
        ICampaignForecastRepository forecastRepository,
        SimulatorForecastService simulatorForecastService,
        TimeProvider timeProvider)
    {
        _bookingRequestRepository = bookingRequestRepository;
        _forecastRepository = forecastRepository;
        _simulatorForecastService = simulatorForecastService;
        _timeProvider = timeProvider;
    }

    public async Task<BookingRequestService.ServiceResult<CampaignForecastDto>> GenerateAsync(
        Guid bookingRequestId,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = await _bookingRequestRepository.GetByIdAsync(bookingRequestId);
        if (bookingRequest is null)
        {
            return BookingRequestService.ServiceResult<CampaignForecastDto>.Success(null);
        }

        if (bookingRequest.DateFrom > bookingRequest.DateTo)
        {
            return BookingRequestService.ServiceResult<CampaignForecastDto>.Failure(
                "DateFrom must be before or equal to DateTo.");
        }

        if (bookingRequest.CreativeDurationSeconds <= 0)
        {
            return BookingRequestService.ServiceResult<CampaignForecastDto>.Failure(
                "CreativeDurationSeconds must be greater than 0.");
        }

        var forecast = await _simulatorForecastService.ForecastAsync(
            new SimulatorForecastRequest(
                bookingRequest.AdvertiserId.ToString(),
                DateOnly.FromDateTime(bookingRequest.DateFrom),
                DateOnly.FromDateTime(bookingRequest.DateTo),
                bookingRequest.Cities,
                bookingRequest.BuildingTypes,
                bookingRequest.ScreenOrientations,
                bookingRequest.CreativeDurationSeconds,
                bookingRequest.Budget,
                bookingRequest.CampaignObjective,
                bookingRequest.Notes),
            cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var persisted = await _forecastRepository.UpsertAsync(new CampaignForecast
        {
            Id = Guid.NewGuid(),
            BookingRequestId = bookingRequestId,
            EligibleScreens = forecast.EligibleScreens,
            EligibleBuildings = forecast.EligibleBuildings,
            EstimatedPlays = forecast.EstimatedPlays,
            EstimatedAudience = forecast.EstimatedAudience,
            EstimatedCost = forecast.EstimatedCost,
            AvailableCapacity = forecast.AvailableCapacity,
            Warnings = forecast.Warnings,
            Conflicts = forecast.Conflicts,
            CreatedAt = now,
            UpdatedAt = now
        });

        return BookingRequestService.ServiceResult<CampaignForecastDto>.Success(Map(persisted));
    }

    public async Task<CampaignForecastDto?> GetLatestAsync(Guid bookingRequestId)
    {
        var forecast = await _forecastRepository.GetByBookingRequestIdAsync(bookingRequestId);
        return forecast is null ? null : Map(forecast);
    }

    private static CampaignForecastDto Map(CampaignForecast item) =>
        new(
            item.Id,
            item.BookingRequestId,
            item.EligibleScreens,
            item.EligibleBuildings,
            item.EstimatedPlays,
            item.EstimatedAudience,
            item.EstimatedCost,
            item.AvailableCapacity,
            item.Warnings,
            item.Conflicts,
            item.CreatedAt,
            item.UpdatedAt);
}
