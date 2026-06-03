using ElevatorAds.Application.Campaigns;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Infrastructure.Repositories;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Campaigns;

public class CampaignEligibilityServiceTests
{
    [Fact]
    public async Task EmptyConstraints_MatchAll()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null),
            async (campaignId, service) =>
            {
                var isEligible = await service.IsEligibleAsync(
                    campaignId,
                    "Lisbon",
                    BuildingType.Residential,
                    ScreenOrientation.Portrait,
                    new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

                Assert.True(isEligible);
            });
    }

    [Fact]
    public async Task CityConstraint_MatchesExpectedCity()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                new[] { "Lisbon" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null),
            async (campaignId, service) =>
            {
                var isEligible = await service.IsEligibleAsync(
                    campaignId,
                    "lisbon",
                    BuildingType.Residential,
                    ScreenOrientation.Portrait,
                    new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

                Assert.True(isEligible);
            });
    }

    [Fact]
    public async Task CityConstraint_RejectsOtherCity()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                new[] { "Lisbon" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null),
            async (campaignId, service) =>
            {
                var isEligible = await service.IsEligibleAsync(
                    campaignId,
                    "Porto",
                    BuildingType.Residential,
                    ScreenOrientation.Portrait,
                    new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

                Assert.False(isEligible);
            });
    }

    [Fact]
    public async Task BuildingTypeConstraint_MatchesExpectedBuildingType()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                Array.Empty<string>(),
                new[] { "Corporate" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null),
            async (campaignId, service) =>
            {
                var isEligible = await service.IsEligibleAsync(
                    campaignId,
                    "Lisbon",
                    BuildingType.Corporate,
                    ScreenOrientation.Portrait,
                    new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

                Assert.True(isEligible);
            });
    }

    [Fact]
    public async Task ScreenOrientationConstraint_MatchesExpectedOrientation()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { "Landscape" },
                Array.Empty<string>(),
                null,
                null),
            async (campaignId, service) =>
            {
                var isEligible = await service.IsEligibleAsync(
                    campaignId,
                    "Lisbon",
                    BuildingType.Residential,
                    ScreenOrientation.Landscape,
                    new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

                Assert.True(isEligible);
            });
    }

    [Fact]
    public async Task DayOfWeekConstraint_Works()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { "Monday" },
                null,
                null),
            async (campaignId, service) =>
            {
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
            });
    }

    [Fact]
    public async Task TimeWindowConstraint_Works()
    {
        await RunWithServiceAsync(
            new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                new TimeOnly(9, 0),
                new TimeOnly(18, 0)),
            async (campaignId, service) =>
            {
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
            });
    }

    private static async Task RunWithServiceAsync(
        CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest request,
        Func<Guid, CampaignEligibilityService, Task> assertion)
    {
        using var fixture = new PersistenceTestFixture();
        var campaignRepository = new EfCampaignRepository(fixture.Context);
        var constraintsRepository = new EfCampaignDeliveryConstraintsRepository(fixture.Context);
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

        var service = new CampaignEligibilityService(constraintsRepository);
        await assertion(campaignId, service);
    }
}
