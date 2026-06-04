using ElevatorAds.Application.Auth;
using ElevatorAds.Domain.Common;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorAds.Api.Auth;

public static class DemoDataSeeder
{
    private const string DefaultOrganizationSlug = "default";
    private const string DemoOrganizationSlug = "demo-corp";
    private const int BuildingCount = 30;
    private const int ScreensPerBuilding = 2;
    private const int AdvertiserCount = 25;
    private const int CreativeCount = 40;
    private const int CampaignCount = 21;
    private const int ProofOfPlayEventCount = 200;

    public static async Task SeedDemoDataAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var config = provider.GetRequiredService<IConfiguration>();
        var dbContext = provider.GetRequiredService<AppDbContext>();

        if (string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            logger.LogInformation("Skipping demo data seeding for in-memory database.");
            return;
        }

        var organizationRepository = provider.GetRequiredService<IOrganizationRepository>();
        var buildingRepository = provider.GetRequiredService<IBuildingRepository>();
        var screenRepository = provider.GetRequiredService<IScreenRepository>();
        var advertiserRepository = provider.GetRequiredService<IAdvertiserRepository>();
        var creativeRepository = provider.GetRequiredService<ICreativeRepository>();
        var campaignRepository = provider.GetRequiredService<ICampaignRepository>();
        var campaignCreativeRepository = provider.GetRequiredService<ICampaignCreativeRepository>();
        var constraintsRepository = provider.GetRequiredService<ICampaignDeliveryConstraintsRepository>();
        var playlistRepository = provider.GetRequiredService<IDailyPlaylistRepository>();
        var proofOfPlayRepository = provider.GetRequiredService<IProofOfPlayEventRepository>();
        var userRepository = provider.GetRequiredService<IUserRepository>();

        var (_, totalBuildings) = await buildingRepository.GetPagedAsync(new PagedQuery(Page: 1, PageSize: 1));
        if (totalBuildings >= BuildingCount)
        {
            logger.LogInformation("Demo data already seeded.");
            return;
        }

        var defaultOrganization = await organizationRepository.GetBySlugAsync(DefaultOrganizationSlug);
        if (defaultOrganization is null)
        {
            throw new InvalidOperationException("Default organization was not seeded before demo data.");
        }

        var rng = new Random(42);
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        logger.LogInformation("Seeding demo DOOH data.");

        var organizations = await SeedOrganizationsAsync(organizationRepository, defaultOrganization, now);
        var buildings = await SeedBuildingsAsync(buildingRepository, organizations, now, rng);
        var screens = await SeedScreensAsync(screenRepository, buildings, now, rng);
        var advertisers = await SeedAdvertisersAsync(advertiserRepository, organizations, now);
        var creatives = await SeedCreativesAsync(creativeRepository, advertisers, now, rng);
        var campaigns = await SeedCampaignsAsync(campaignRepository, advertisers, now, today);
        await SeedCampaignCreativesAsync(campaignCreativeRepository, campaigns, creatives, now);
        await SeedCampaignConstraintsAsync(constraintsRepository, campaigns, buildings, screens, now);
        var playlists = await SeedPlaylistsAsync(playlistRepository, campaigns, creatives, screens, now, today, rng);
        await SeedProofOfPlayAsync(proofOfPlayRepository, playlists, screens, now, today, rng);
        await SeedUsersAsync(userRepository, config, now);

        logger.LogInformation(
            "Seeded demo data: {Buildings} buildings, {Screens} screens, {Advertisers} advertisers, {Creatives} creatives, {Campaigns} campaigns, {Playlists} playlists, {ProofEvents} proof-of-play events.",
            buildings.Count,
            screens.Count,
            advertisers.Count,
            creatives.Count,
            campaigns.Count,
            playlists.Count,
            ProofOfPlayEventCount);
    }

    private static async Task<List<Organization>> SeedOrganizationsAsync(
        IOrganizationRepository organizationRepository,
        Organization defaultOrganization,
        DateTime now)
    {
        var demoOrganization = await organizationRepository.GetBySlugAsync(DemoOrganizationSlug);
        if (demoOrganization is null)
        {
            demoOrganization = await organizationRepository.AddAsync(new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Demo Corp",
                Slug = DemoOrganizationSlug,
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        return [defaultOrganization, demoOrganization];
    }

    private static async Task<List<Building>> SeedBuildingsAsync(
        IBuildingRepository buildingRepository,
        IReadOnlyList<Organization> organizations,
        DateTime now,
        Random rng)
    {
        var buildingNames = new[]
        {
            "Aurora Residences", "Atlas Center", "Mercury Plaza", "Lighthouse Tower", "Harbor Point",
            "Northline Offices", "Jardins Residence", "Broadway Exchange", "Maple Square", "Riverside Hub",
            "Vista Corporate", "Summit Atrium", "Oceanview Gardens", "Parkside Commons", "Santos Gateway",
            "Liberty Lofts", "Central Quarters", "Pinecrest Tower", "Marina Heights", "Cedar House",
            "Infinity Place", "Porto Vista", "Granite Center", "Elm Street Court", "Terrace 25",
            "Metro West", "Avenida Prime", "Hilltop Suites", "Station One", "Solaris Building"
        };
        var cities = new[]
        {
            "Lisbon", "Porto", "Braga", "Coimbra", "Faro",
            "Setubal", "Aveiro", "Leiria", "Viseu", "Evora"
        };
        var streets = new[]
        {
            "Avenida Central", "Rua das Flores", "Praca do Comercio", "Avenida da Liberdade", "Rua do Carmo",
            "Rua das Oliveiras", "Travessa do Sol", "Avenida do Atlantico", "Rua do Teatro", "Praca Verde"
        };
        var buildingTypes = Enum.GetValues<BuildingType>();
        var buildings = new List<Building>(BuildingCount);

        for (var i = 0; i < BuildingCount; i++)
        {
            var organization = organizations[i < BuildingCount / 2 ? 0 : 1];
            var city = cities[i % cities.Length];
            var addressNumber = 100 + (i * 7);
            var createdAt = now.AddMinutes(-(BuildingCount - i));

            var building = new Building
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = buildingNames[i],
                Address = $"{streets[i % streets.Length]}, {addressNumber}",
                City = city,
                Country = "Portugal",
                PostalCode = $"1{(i % 9)}{(i % 7)}{(i % 5)}-2{(i % 8)}{(i % 6)}",
                BuildingType = buildingTypes[i % buildingTypes.Length],
                EstimatedDailyAudience = 80 + rng.Next(40, 420),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            buildings.Add(await buildingRepository.AddAsync(building));
        }

        return buildings;
    }

    private static async Task<List<Screen>> SeedScreensAsync(
        IScreenRepository screenRepository,
        IReadOnlyList<Building> buildings,
        DateTime now,
        Random rng)
    {
        var screens = new List<Screen>(buildings.Count * ScreensPerBuilding);

        for (var buildingIndex = 0; buildingIndex < buildings.Count; buildingIndex++)
        {
            var building = buildings[buildingIndex];

            for (var offset = 0; offset < ScreensPerBuilding; offset++)
            {
                var screenIndex = (buildingIndex * ScreensPerBuilding) + offset;
                var isPortrait = (screenIndex % 3) == 1;
                var status = screenIndex switch
                {
                    < 48 => ScreenStatus.Active,
                    < 54 => ScreenStatus.Inactive,
                    _ => ScreenStatus.Maintenance
                };
                var createdAt = now.AddMinutes(-(150 + screenIndex));

                var screen = new Screen
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = building.OrganizationId,
                    BuildingId = building.Id,
                    Name = $"{building.Name} Screen {offset + 1}",
                    ExternalCode = $"SCR-{screenIndex + 1:D4}",
                    ResolutionWidth = isPortrait ? 1080 : 1920,
                    ResolutionHeight = isPortrait ? 1920 : 1080,
                    Orientation = isPortrait ? ScreenOrientation.Portrait : ScreenOrientation.Landscape,
                    Status = status,
                    LastSeenAt = status == ScreenStatus.Active ? now.AddMinutes(-rng.Next(5, 180)) : now.AddDays(-rng.Next(2, 10)),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };

                screens.Add(await screenRepository.AddAsync(screen));
            }
        }

        return screens;
    }

    private static async Task<List<Advertiser>> SeedAdvertisersAsync(
        IAdvertiserRepository advertiserRepository,
        IReadOnlyList<Organization> organizations,
        DateTime now)
    {
        var companyNames = new[]
        {
            "Aurora Fitness", "Blue Harbor Bank", "Cedar Homes", "Delta Mobility", "Evergreen Health",
            "Flux Telecom", "Golden Bean Coffee", "Helix Insurance", "Ion Travel", "Juno Market",
            "Keystone Energy", "Luma Hotels", "Metro Grocers", "North Peak Apparel", "Oliveira Clinics",
            "Pulse Cinemas", "Quanta Learning", "Ridge Finance", "Solara Kitchens", "Tidal Airlines",
            "Urban Bloom", "Vector Security", "Westbridge Legal", "Xenon Labs", "Yield Ventures"
        };

        var advertisers = new List<Advertiser>(AdvertiserCount);
        for (var i = 0; i < AdvertiserCount; i++)
        {
            var organization = organizations[i % organizations.Count];
            var createdAt = now.AddMinutes(-(250 + i));

            var advertiser = new Advertiser
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = companyNames[i],
                LegalName = $"{companyNames[i]} Ltd.",
                TaxId = $"PT-{100000000 + i}",
                ContactName = $"Contact {i + 1}",
                ContactEmail = $"contact{i + 1}@demo-dooh.test",
                Phone = $"+351 21{(3000000 + (i * 137)):D7}",
                Status = i < 20 ? AdvertiserStatus.Active : AdvertiserStatus.Inactive,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            advertisers.Add(await advertiserRepository.AddAsync(advertiser));
        }

        return advertisers;
    }

    private static async Task<List<Creative>> SeedCreativesAsync(
        ICreativeRepository creativeRepository,
        IReadOnlyList<Advertiser> advertisers,
        DateTime now,
        Random rng)
    {
        var approvedAdvertiserSlots = new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4 };
        var statuses = new[]
        {
            ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved,
            ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved,
            ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved, ApprovalStatus.Approved,
            ApprovalStatus.Approved, ApprovalStatus.Approved,
            ApprovalStatus.PendingReview, ApprovalStatus.PendingReview, ApprovalStatus.PendingReview, ApprovalStatus.PendingReview,
            ApprovalStatus.PendingReview, ApprovalStatus.PendingReview, ApprovalStatus.PendingReview, ApprovalStatus.PendingReview,
            ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft,
            ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft, ApprovalStatus.Draft,
            ApprovalStatus.Rejected, ApprovalStatus.Rejected, ApprovalStatus.Rejected, ApprovalStatus.Rejected,
            ApprovalStatus.Rejected, ApprovalStatus.Rejected, ApprovalStatus.Rejected, ApprovalStatus.Rejected
        };

        var creatives = new List<Creative>(CreativeCount);
        var fallbackAdvertiserCursor = 5;

        for (var i = 0; i < CreativeCount; i++)
        {
            var advertiserIndex = i < approvedAdvertiserSlots.Length
                ? approvedAdvertiserSlots[i]
                : fallbackAdvertiserCursor++ % advertisers.Count;
            var advertiser = advertisers[advertiserIndex];
            var isVideo = i % 3 != 0;
            var createdAt = now.AddMinutes(-(320 + i));

            var creative = new Creative
            {
                Id = Guid.NewGuid(),
                OrganizationId = advertiser.OrganizationId,
                AdvertiserId = advertiser.Id,
                Name = $"{advertiser.Name} Creative {i + 1:D2}",
                MediaUrl = $"https://cdn.demo-dooh.test/{advertiser.Name.ToLowerInvariant().Replace(' ', '-')}/creative-{i + 1:D2}.{(isVideo ? "mp4" : "jpg")}",
                MediaType = isVideo ? MediaType.Video : MediaType.Image,
                DurationSeconds = isVideo ? 15 + (i % 4) * 5 : 10 + (i % 3) * 5,
                ApprovalStatus = statuses[i],
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddHours(rng.Next(1, 72))
            };

            creatives.Add(await creativeRepository.AddAsync(creative));
        }

        return creatives;
    }

    private static async Task<List<Campaign>> SeedCampaignsAsync(
        ICampaignRepository campaignRepository,
        IReadOnlyList<Advertiser> advertisers,
        DateTime now,
        DateOnly today)
    {
        var assignedAdvertiserIndexes = new[] { 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 4 };
        var campaigns = new List<Campaign>(CampaignCount);

        for (var i = 0; i < CampaignCount; i++)
        {
            var advertiser = advertisers[assignedAdvertiserIndexes[i]];
            var status = i switch
            {
                < 8 => CampaignStatus.Active,
                < 12 => CampaignStatus.Paused,
                < 16 => CampaignStatus.Completed,
                _ => CampaignStatus.Draft
            };

            var (startDate, endDate) = status switch
            {
                CampaignStatus.Active => (
                    today.AddDays(-(12 - (i % 4))).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc),
                    today.AddDays(12 + (i % 5)).ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)),
                CampaignStatus.Paused => (
                    today.AddDays(-(6 + (i % 3))).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc),
                    today.AddDays(8 + (i % 4)).ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)),
                CampaignStatus.Completed => (
                    today.AddDays(-(40 + i)).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc),
                    today.AddDays(-(8 + (i % 4))).ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)),
                _ => (
                    today.AddDays(5 + (i % 6)).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc),
                    today.AddDays(22 + (i % 7)).ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc))
            };

            var createdAt = now.AddMinutes(-(400 + i));
            var campaign = new Campaign
            {
                Id = Guid.NewGuid(),
                OrganizationId = advertiser.OrganizationId,
                AdvertiserId = advertiser.Id,
                Name = $"{advertiser.Name} Campaign {i + 1:D2}",
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                DailyBudget = 80 + (i * 10),
                TotalBudget = 1800 + (i * 125),
                MaxCpm = 6 + (i % 5),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            campaigns.Add(await campaignRepository.AddAsync(campaign));
        }

        return campaigns;
    }

    private static async Task SeedCampaignCreativesAsync(
        ICampaignCreativeRepository campaignCreativeRepository,
        IReadOnlyList<Campaign> campaigns,
        IReadOnlyList<Creative> creatives,
        DateTime now)
    {
        var approvedCreativesByAdvertiser = creatives
            .Where(creative => creative.ApprovalStatus == ApprovalStatus.Approved)
            .GroupBy(creative => creative.AdvertiserId)
            .ToDictionary(group => group.Key, group => group.OrderBy(creative => creative.CreatedAt).ToList());

        for (var i = 0; i < campaigns.Count; i++)
        {
            var campaign = campaigns[i];
            if (!approvedCreativesByAdvertiser.TryGetValue(campaign.AdvertiserId, out var options))
            {
                continue;
            }

            var assignmentCount = options.Count >= 3 ? 3 : options.Count;
            for (var creativeIndex = 0; creativeIndex < assignmentCount; creativeIndex++)
            {
                var creative = options[creativeIndex];
                await campaignCreativeRepository.AddAsync(new CampaignCreative
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaign.Id,
                    CreativeId = creative.Id,
                    CreatedAt = now.AddMinutes(-(500 + (i * 5) + creativeIndex))
                });
            }
        }
    }

    private static async Task SeedCampaignConstraintsAsync(
        ICampaignDeliveryConstraintsRepository constraintsRepository,
        IReadOnlyList<Campaign> campaigns,
        IReadOnlyList<Building> buildings,
        IReadOnlyList<Screen> screens,
        DateTime now)
    {
        for (var i = 0; i < campaigns.Count; i++)
        {
            var building = buildings[i % buildings.Count];
            var screen = screens[(i * 2) % screens.Count];

            await constraintsRepository.UpsertAsync(new CampaignDeliveryConstraints
            {
                Id = Guid.NewGuid(),
                CampaignId = campaigns[i].Id,
                Cities = [building.City],
                BuildingTypes = [building.BuildingType],
                ScreenOrientations = [screen.Orientation],
                DaysOfWeek = i < 8
                    ? [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday]
                    : i < 15
                        ? [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]
                        : [DayOfWeek.Saturday, DayOfWeek.Sunday],
                StartTime = new TimeOnly(6 + (i % 4), 0),
                EndTime = new TimeOnly(18 + (i % 4), 0),
                CreatedAt = now.AddMinutes(-(620 + i)),
                UpdatedAt = now.AddMinutes(-(620 + i))
            });
        }
    }

    private static async Task<List<DailyPlaylist>> SeedPlaylistsAsync(
        IDailyPlaylistRepository playlistRepository,
        IReadOnlyList<Campaign> campaigns,
        IReadOnlyList<Creative> creatives,
        IReadOnlyList<Screen> screens,
        DateTime now,
        DateOnly today,
        Random rng)
    {
        var creativesById = creatives.ToDictionary(creative => creative.Id);
        var assignmentsByCampaign = campaigns.ToDictionary(
            campaign => campaign.Id,
            campaign => creatives
                .Where(creative => creative.AdvertiserId == campaign.AdvertiserId && creative.ApprovalStatus == ApprovalStatus.Approved)
                .OrderBy(creative => creative.CreatedAt)
                .Take(3)
                .Select(creative => new CampaignCreative
                {
                    CampaignId = campaign.Id,
                    CreativeId = creative.Id
                })
                .ToList());

        var playlistSeeds = new (int ScreenIndex, DateOnly Date, DailyPlaylistStatus Status)[]
        {
            (0, today.AddDays(-5), DailyPlaylistStatus.Expired),
            (1, today.AddDays(-4), DailyPlaylistStatus.Expired),
            (2, today.AddDays(-3), DailyPlaylistStatus.Downloaded),
            (3, today.AddDays(-2), DailyPlaylistStatus.Downloaded),
            (4, today.AddDays(-1), DailyPlaylistStatus.Published),
            (5, today, DailyPlaylistStatus.Published),
            (6, today, DailyPlaylistStatus.Published)
        };

        var eligibleCampaigns = campaigns
            .Where(campaign => campaign.Status == CampaignStatus.Active)
            .ToList();
        var playlists = new List<DailyPlaylist>(playlistSeeds.Length);
        var campaignCursor = 0;

        foreach (var (screenIndex, date, status) in playlistSeeds)
        {
            var screen = screens[screenIndex];
            var itemCount = 4 + rng.Next(0, 3);
            var createdAt = date.ToDateTime(new TimeOnly(5, 30), DateTimeKind.Utc);
            var items = new List<DailyPlaylistItem>(itemCount);

            for (var order = 0; order < itemCount; order++)
            {
                Campaign? selectedCampaign = null;
                List<CampaignCreative>? assignedCreatives = null;

                for (var attempt = 0; attempt < eligibleCampaigns.Count; attempt++)
                {
                    var candidate = eligibleCampaigns[(campaignCursor + attempt) % eligibleCampaigns.Count];
                    var candidateDate = date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
                    if (candidate.StartDate.HasValue && candidate.StartDate.Value > candidateDate)
                    {
                        continue;
                    }

                    if (candidate.EndDate.HasValue && candidate.EndDate.Value < candidateDate)
                    {
                        continue;
                    }

                    if (assignmentsByCampaign.TryGetValue(candidate.Id, out var candidateAssignments) && candidateAssignments.Count > 0)
                    {
                        selectedCampaign = candidate;
                        assignedCreatives = candidateAssignments;
                        campaignCursor = (campaignCursor + attempt + 1) % eligibleCampaigns.Count;
                        break;
                    }
                }

                if (selectedCampaign is null || assignedCreatives is null)
                {
                    continue;
                }

                var assignment = assignedCreatives[order % assignedCreatives.Count];
                var creative = creativesById[assignment.CreativeId];
                items.Add(new DailyPlaylistItem
                {
                    Id = Guid.NewGuid(),
                    DailyPlaylistId = Guid.Empty,
                    CampaignId = selectedCampaign.Id,
                    CreativeId = creative.Id,
                    Order = order,
                    DurationSeconds = creative.DurationSeconds,
                    CreatedAt = createdAt.AddMinutes(order * 3)
                });
            }

            var playlist = new DailyPlaylist
            {
                Id = Guid.NewGuid(),
                OrganizationId = screen.OrganizationId,
                ScreenId = screen.Id,
                Date = date,
                Version = 1,
                Status = status,
                GeneratedAt = createdAt,
                PublishedAt = status is DailyPlaylistStatus.Published or DailyPlaylistStatus.Downloaded or DailyPlaylistStatus.Expired
                    ? createdAt.AddMinutes(15)
                    : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Items = items
            };

            foreach (var item in playlist.Items)
            {
                item.DailyPlaylistId = playlist.Id;
            }

            playlists.Add(await playlistRepository.AddAsync(playlist));
        }

        return playlists;
    }

    private static async Task SeedProofOfPlayAsync(
        IProofOfPlayEventRepository proofOfPlayRepository,
        IReadOnlyList<DailyPlaylist> playlists,
        IReadOnlyList<Screen> screens,
        DateTime now,
        DateOnly today,
        Random rng)
    {
        var playablePlaylists = playlists.Where(playlist => playlist.Items.Count > 0).ToList();
        var todayPlaylists = playablePlaylists.Where(playlist => playlist.Date == today).ToList();

        for (var i = 0; i < ProofOfPlayEventCount; i++)
        {
            var playlist = i < 40 && todayPlaylists.Count > 0
                ? todayPlaylists[i % todayPlaylists.Count]
                : playablePlaylists[i % playablePlaylists.Count];
            var item = playlist.Items[i % playlist.Items.Count];
            var screen = screens.First(screenItem => screenItem.Id == playlist.ScreenId);
            var dayBase = playlist.Date.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            var playedAt = dayBase
                .AddMinutes((i * 11) % 720)
                .AddSeconds(rng.Next(0, 55));
            if (playedAt > now)
            {
                playedAt = now.AddMinutes(-((i % 30) + 1));
            }

            await proofOfPlayRepository.AddAsync(new ProofOfPlayEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = playlist.OrganizationId,
                ScreenId = screen.Id,
                PlaylistId = playlist.Id,
                PlaylistItemId = item.Id,
                CampaignId = item.CampaignId,
                CreativeId = item.CreativeId,
                PlayedAt = playedAt,
                DurationSeconds = item.DurationSeconds,
                CreatedAt = playedAt.AddSeconds(5)
            });
        }
    }

    private static async Task SeedUsersAsync(
        IUserRepository userRepository,
        IConfiguration config,
        DateTime now)
    {
        var demoUsers = new[]
        {
            new
            {
                Username = "operator",
                Password = Environment.GetEnvironmentVariable("SEED_OPERATOR_PASSWORD")
                    ?? config["Auth:SeedOperatorPassword"]
                    ?? "Operator1!",
                Role = UserRole.Operator
            },
            new
            {
                Username = "viewer",
                Password = Environment.GetEnvironmentVariable("SEED_VIEWER_PASSWORD")
                    ?? config["Auth:SeedViewerPassword"]
                    ?? "Viewer1!",
                Role = UserRole.Viewer
            }
        };

        foreach (var demoUser in demoUsers)
        {
            if (await userRepository.GetByUsernameAsync(demoUser.Username) is not null)
            {
                continue;
            }

            await userRepository.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Username = demoUser.Username,
                PasswordHash = PasswordHasher.Hash(demoUser.Password),
                Role = demoUser.Role,
                CreatedAt = now
            });
        }
    }
}
