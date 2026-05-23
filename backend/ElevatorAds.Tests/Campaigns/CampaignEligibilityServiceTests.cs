using ElevatorAds.Application.Campaigns;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Infrastructure.Repositories;

namespace ElevatorAds.Tests.Campaigns;

public class CampaignEligibilityServiceTests
{
    [Fact]
    public async Task EmptyConstraints_MatchAll()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null));

        var isEligible = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(isEligible);
    }

    [Fact]
    public async Task CityConstraint_MatchesExpectedCity()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            new[] { "Lisbon" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null));

        var isEligible = await service.IsEligibleAsync(
            campaignId,
            "lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(isEligible);
    }

    [Fact]
    public async Task CityConstraint_RejectsOtherCity()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            new[] { "Lisbon" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null));

        var isEligible = await service.IsEligibleAsync(
            campaignId,
            "Porto",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(isEligible);
    }

    [Fact]
    public async Task BuildingTypeConstraint_MatchesExpectedBuildingType()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            new[] { "Corporate" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null));

        var isEligible = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Corporate,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(isEligible);
    }

    [Fact]
    public async Task ScreenOrientationConstraint_MatchesExpectedOrientation()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { "Landscape" },
            Array.Empty<string>(),
            null,
            null));

        var isEligible = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Landscape,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(isEligible);
    }

    [Fact]
    public async Task DayOfWeekConstraint_Works()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { "Monday" },
            null,
            null));

        var mondayEligible = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));
        var tuesdayEligible = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(mondayEligible);
        Assert.False(tuesdayEligible);
    }

    [Fact]
    public async Task TimeWindowConstraint_Works()
    {
        var (campaignId, service) = await CreateServiceAsync(new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)));

        var duringWindow = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var beforeWindow = await service.IsEligibleAsync(
            campaignId,
            "Lisbon",
            BuildingType.Residential,
            ScreenOrientation.Portrait,
            new DateTime(2026, 6, 1, 8, 59, 0, DateTimeKind.Utc));

        Assert.True(duringWindow);
        Assert.False(beforeWindow);
    }

    private static async Task<(Guid CampaignId, CampaignEligibilityService Service)> CreateServiceAsync(
        CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest request)
    {
        var campaignRepository = new InMemoryCampaignRepository();
        var constraintsRepository = new InMemoryCampaignDeliveryConstraintsRepository();
        var campaignId = Guid.NewGuid();

        await campaignRepository.AddAsync(new Campaign
        {
            Id = campaignId,
            AdvertiserId = Guid.NewGuid(),
            Name = "Campaign",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var constraintsService = new CampaignDeliveryConstraintsService(campaignRepository, constraintsRepository);
        var upsertResult = await constraintsService.UpsertAsync(campaignId, request);
        Assert.True(upsertResult.IsSuccess);

        return (campaignId, new CampaignEligibilityService(constraintsRepository));
    }
}
