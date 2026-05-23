using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Campaigns;

public sealed class CampaignEligibilityService
{
    private readonly ICampaignDeliveryConstraintsRepository _constraintsRepository;

    public CampaignEligibilityService(ICampaignDeliveryConstraintsRepository constraintsRepository) =>
        _constraintsRepository = constraintsRepository;

    public async Task<bool> IsEligibleAsync(
        Guid campaignId,
        string city,
        BuildingType buildingType,
        ScreenOrientation screenOrientation,
        DateTime currentDateTime)
    {
        var constraints = await _constraintsRepository.GetByCampaignIdAsync(campaignId);
        if (constraints is null)
        {
            return true;
        }

        if (constraints.Cities.Count > 0 &&
            !constraints.Cities.Contains(city, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (constraints.BuildingTypes.Count > 0 && !constraints.BuildingTypes.Contains(buildingType))
        {
            return false;
        }

        if (constraints.ScreenOrientations.Count > 0 && !constraints.ScreenOrientations.Contains(screenOrientation))
        {
            return false;
        }

        if (constraints.DaysOfWeek.Count > 0 && !constraints.DaysOfWeek.Contains(currentDateTime.DayOfWeek))
        {
            return false;
        }

        if (constraints.StartTime.HasValue && constraints.EndTime.HasValue)
        {
            var currentTime = TimeOnly.FromDateTime(currentDateTime);
            if (currentTime < constraints.StartTime.Value || currentTime > constraints.EndTime.Value)
            {
                return false;
            }
        }

        return true;
    }
}
