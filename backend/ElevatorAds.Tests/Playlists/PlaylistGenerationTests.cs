using ElevatorAds.Application.Campaigns;
using ElevatorAds.Application.Playlists;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Repositories;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Playlists;

public class PlaylistGenerationTests
{
    [Fact]
    public async Task Generate_EmptyEligibleCampaigns_ReturnsDraftEmptyPlaylist()
    {
        var context = await CreateContextAsync();

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        Assert.Equal(DailyPlaylistStatus.Draft, playlist.Status);
        Assert.Empty(playlist.Items);
    }

    [Fact]
    public async Task Generate_WithEligibleCampaign_CreatesPlaylistItems()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        await context.AssignCreativeAsync(campaign.Id, creative.Id);

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        var item = Assert.Single(playlist.Items);
        Assert.Equal(campaign.Id, item.CampaignId);
        Assert.Equal(creative.Id, item.CreativeId);
        Assert.Equal(15, item.DurationSeconds);
    }

    [Fact]
    public async Task Generate_ExcludesCampaignsByDeliveryConstraints()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        await context.AssignCreativeAsync(campaign.Id, creative.Id);
        await context.UpsertConstraintsAsync(campaign.Id, cities: ["Porto"]);

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        Assert.Empty(playlist.Items);
    }

    [Fact]
    public async Task Generate_ExcludesUnapprovedCreatives()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var approvedCreative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        var draftCreative = await context.AddCreativeAsync(ApprovalStatus.Draft, 20);
        await context.AssignCreativeAsync(campaign.Id, approvedCreative.Id);
        await context.AssignCreativeAsync(campaign.Id, draftCreative.Id);

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        var item = Assert.Single(playlist.Items);
        Assert.Equal(approvedCreative.Id, item.CreativeId);
    }

    [Fact]
    public async Task Generate_UsesOnlyAssignedCreatives()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var assignedCreative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        var unassignedCreative = await context.AddCreativeAsync(ApprovalStatus.Approved, 20);
        await context.AssignCreativeAsync(campaign.Id, assignedCreative.Id);

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        Assert.DoesNotContain(playlist.Items, item => item.CreativeId == unassignedCreative.Id);
    }

    [Fact]
    public async Task Generate_ItemsAreDeterministicallyOrdered()
    {
        var context = await CreateContextAsync();
        var firstCampaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var secondCampaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creativeA = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        var creativeB = await context.AddCreativeAsync(ApprovalStatus.Approved, 20);
        var creativeC = await context.AddCreativeAsync(ApprovalStatus.Approved, 25);
        await context.AssignCreativeAsync(firstCampaign.Id, creativeB.Id);
        await context.AssignCreativeAsync(firstCampaign.Id, creativeA.Id);
        await context.AssignCreativeAsync(secondCampaign.Id, creativeC.Id);

        var firstRun = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));
        var secondRun = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var firstItems = Assert.Single(firstRun).Items;
        var secondItems = Assert.Single(secondRun).Items;
        Assert.Equal([0, 1, 2], firstItems.Select(item => item.Order).ToArray());
        Assert.Equal(
            firstItems.Select(item => (item.CampaignId, item.CreativeId)),
            secondItems.Select(item => (item.CampaignId, item.CreativeId)));
    }

    [Fact]
    public async Task Generate_IncrementsVersion_OnRegenerate()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        await context.AssignCreativeAsync(campaign.Id, creative.Id);

        var firstRun = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));
        var secondRun = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        Assert.Equal(1, Assert.Single(firstRun).Version);
        Assert.Equal(2, Assert.Single(secondRun).Version);
    }

    [Fact]
    public async Task Publish_TransitionsStatus()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        await context.AssignCreativeAsync(campaign.Id, creative.Id);
        var generated = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var published = await context.Service.PublishAsync(Assert.Single(generated).Id);

        Assert.NotNull(published);
        Assert.Equal(DailyPlaylistStatus.Published, published!.Status);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task GetByScreenAndDate_ReturnsCorrectPlaylist()
    {
        var context = await CreateContextAsync();
        var campaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var creative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        await context.AssignCreativeAsync(campaign.Id, creative.Id);
        var date = new DateOnly(2026, 6, 1);
        var generated = await context.Service.GenerateAsync(date);

        var playlist = await context.PlaylistRepository.GetByScreenAndDateAsync(context.Screen.Id, date);

        Assert.NotNull(playlist);
        Assert.Equal(Assert.Single(generated).Id, playlist!.Id);
    }

    [Fact]
    public async Task Generate_ActiveCampaignsOnly()
    {
        var context = await CreateContextAsync();
        var activeCampaign = await context.AddCampaignAsync(CampaignStatus.Active);
        var pausedCampaign = await context.AddCampaignAsync(CampaignStatus.Paused);
        var activeCreative = await context.AddCreativeAsync(ApprovalStatus.Approved, 15);
        var pausedCreative = await context.AddCreativeAsync(ApprovalStatus.Approved, 20);
        await context.AssignCreativeAsync(activeCampaign.Id, activeCreative.Id);
        await context.AssignCreativeAsync(pausedCampaign.Id, pausedCreative.Id);

        var playlists = await context.Service.GenerateAsync(new DateOnly(2026, 6, 1));

        var playlist = Assert.Single(playlists);
        var item = Assert.Single(playlist.Items);
        Assert.Equal(activeCampaign.Id, item.CampaignId);
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        var fixture = new PersistenceTestFixture();
        var buildingRepository = new EfBuildingRepository(fixture.Context);
        var screenRepository = new EfScreenRepository(fixture.Context);
        var campaignRepository = new EfCampaignRepository(fixture.Context);
        var campaignCreativeRepository = new EfCampaignCreativeRepository(fixture.Context);
        var creativeRepository = new EfCreativeRepository(fixture.Context);
        var constraintsRepository = new EfCampaignDeliveryConstraintsRepository(fixture.Context);
        var playlistRepository = new EfDailyPlaylistRepository(fixture.Context);

        var building = new Building
        {
            Id = Guid.NewGuid(),
            Name = "Tower One",
            Address = "Rua A",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000-001",
            BuildingType = BuildingType.Residential,
            EstimatedDailyAudience = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await buildingRepository.AddAsync(building);

        var screen = new Screen
        {
            Id = Guid.NewGuid(),
            BuildingId = building.Id,
            Name = "Screen A",
            ExternalCode = "SCR-001",
            ResolutionWidth = 1080,
            ResolutionHeight = 1920,
            Orientation = ScreenOrientation.Portrait,
            Status = ScreenStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await screenRepository.AddAsync(screen);

        var eligibilityService = new CampaignEligibilityService(constraintsRepository);
        var service = new PlaylistGenerationService(
            screenRepository,
            buildingRepository,
            campaignRepository,
            campaignCreativeRepository,
            creativeRepository,
            playlistRepository,
            eligibilityService);

        return new TestContext(
            service,
            playlistRepository,
            campaignRepository,
            campaignCreativeRepository,
            creativeRepository,
            constraintsRepository,
            building,
            screen,
            fixture);
    }

    private sealed class TestContext : IDisposable
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ICampaignCreativeRepository _campaignCreativeRepository;
        private readonly ICreativeRepository _creativeRepository;
        private readonly ICampaignDeliveryConstraintsRepository _constraintsRepository;
        private readonly PersistenceTestFixture _fixture;

        public TestContext(
            PlaylistGenerationService service,
            IDailyPlaylistRepository playlistRepository,
            ICampaignRepository campaignRepository,
            ICampaignCreativeRepository campaignCreativeRepository,
            ICreativeRepository creativeRepository,
            ICampaignDeliveryConstraintsRepository constraintsRepository,
            Building building,
            Screen screen,
            PersistenceTestFixture fixture)
        {
            Service = service;
            PlaylistRepository = playlistRepository;
            _campaignRepository = campaignRepository;
            _campaignCreativeRepository = campaignCreativeRepository;
            _creativeRepository = creativeRepository;
            _constraintsRepository = constraintsRepository;
            Building = building;
            Screen = screen;
            _fixture = fixture;
        }

        public void Dispose() => _fixture.Dispose();

        public PlaylistGenerationService Service { get; }
        public IDailyPlaylistRepository PlaylistRepository { get; }
        public Building Building { get; }
        public Screen Screen { get; }

        public Task<Campaign> AddCampaignAsync(CampaignStatus status)
        {
            var campaign = new Campaign
            {
                Id = Guid.NewGuid(),
                AdvertiserId = Guid.NewGuid(),
                Name = $"Campaign-{status}",
                StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return _campaignRepository.AddAsync(campaign);
        }

        public Task<Creative> AddCreativeAsync(ApprovalStatus approvalStatus, int durationSeconds)
        {
            var creative = new Creative
            {
                Id = Guid.NewGuid(),
                AdvertiserId = Guid.NewGuid(),
                Name = $"Creative-{approvalStatus}-{durationSeconds}",
                MediaUrl = $"https://cdn.example.com/{Guid.NewGuid():N}.jpg",
                MediaType = MediaType.Image,
                DurationSeconds = durationSeconds,
                ApprovalStatus = approvalStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return _creativeRepository.AddAsync(creative);
        }

        public Task<CampaignCreative> AssignCreativeAsync(Guid campaignId, Guid creativeId)
        {
            var assignment = new CampaignCreative
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                CreativeId = creativeId,
                CreatedAt = DateTime.UtcNow
            };

            return _campaignCreativeRepository.AddAsync(assignment);
        }

        public Task UpsertConstraintsAsync(
            Guid campaignId,
            string[]? cities = null,
            string[]? buildingTypes = null,
            string[]? screenOrientations = null,
            string[]? daysOfWeek = null,
            TimeOnly? startTime = null,
            TimeOnly? endTime = null)
        {
            var service = new CampaignDeliveryConstraintsService(_campaignRepository, _constraintsRepository);

            return UpsertAsync(service, campaignId, cities, buildingTypes, screenOrientations, daysOfWeek, startTime, endTime);
        }

        private static async Task UpsertAsync(
            CampaignDeliveryConstraintsService service,
            Guid campaignId,
            string[]? cities,
            string[]? buildingTypes,
            string[]? screenOrientations,
            string[]? daysOfWeek,
            TimeOnly? startTime,
            TimeOnly? endTime)
        {
            var result = await service.UpsertAsync(
                campaignId,
                new CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest(
                    cities ?? [],
                    buildingTypes ?? [],
                    screenOrientations ?? [],
                    daysOfWeek ?? [],
                    startTime,
                    endTime));

            Assert.True(result.IsSuccess);
        }
    }
}
