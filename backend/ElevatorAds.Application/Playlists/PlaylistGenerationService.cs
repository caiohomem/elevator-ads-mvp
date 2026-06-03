using ElevatorAds.Application.Campaigns;
using ElevatorAds.Domain.Common;
using ElevatorAds.Application.Playlists.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Playlists;

public sealed class PlaylistGenerationService
{
    private readonly IScreenRepository _screenRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignCreativeRepository _campaignCreativeRepository;
    private readonly ICreativeRepository _creativeRepository;
    private readonly IDailyPlaylistRepository _playlistRepository;
    private readonly CampaignEligibilityService _eligibilityService;

    public PlaylistGenerationService(
        IScreenRepository screenRepository,
        IBuildingRepository buildingRepository,
        ICampaignRepository campaignRepository,
        ICampaignCreativeRepository campaignCreativeRepository,
        ICreativeRepository creativeRepository,
        IDailyPlaylistRepository playlistRepository,
        CampaignEligibilityService eligibilityService)
    {
        _screenRepository = screenRepository;
        _buildingRepository = buildingRepository;
        _campaignRepository = campaignRepository;
        _campaignCreativeRepository = campaignCreativeRepository;
        _creativeRepository = creativeRepository;
        _playlistRepository = playlistRepository;
        _eligibilityService = eligibilityService;
    }

    public async Task<IReadOnlyList<DailyPlaylistDto>> GetAllAsync()
    {
        var playlists = await _playlistRepository.GetAllAsync();
        return playlists.Select(Map).ToList();
    }

    public async Task<PagedResult<DailyPlaylistDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _playlistRepository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<DailyPlaylistDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<DailyPlaylistDto?> GetByIdAsync(Guid id)
    {
        var playlist = await _playlistRepository.GetByIdAsync(id);
        return playlist is null ? null : Map(playlist);
    }

    public async Task<IReadOnlyList<DailyPlaylistDto>> GetByScreenIdAsync(Guid screenId, DateOnly? date)
    {
        var playlists = await _playlistRepository.GetByScreenIdAsync(screenId, date);
        return playlists.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DailyPlaylistDto>> GenerateAsync(DateOnly date)
    {
        var screens = (await _screenRepository.GetAllAsync())
            .OrderBy(screen => screen.CreatedAt)
            .ThenBy(screen => screen.Id)
            .ToList();
        var campaigns = (await _campaignRepository.GetAllAsync())
            .Where(campaign => campaign.Status == CampaignStatus.Active)
            .Where(campaign => !campaign.StartDate.HasValue || DateOnly.FromDateTime(campaign.StartDate.Value) <= date)
            .Where(campaign => !campaign.EndDate.HasValue || DateOnly.FromDateTime(campaign.EndDate.Value) >= date)
            .ToList();
        var result = new List<DailyPlaylistDto>();

        foreach (var screen in screens)
        {
            var building = await _buildingRepository.GetByIdAsync(screen.BuildingId);
            if (building is null)
            {
                continue;
            }

            var eligibleEntries = await GetEligibleEntriesAsync(campaigns, building, screen, date);
            var playlist = await UpsertPlaylistAsync(screen.Id, date, eligibleEntries);
            result.Add(Map(playlist));
        }

        return result;
    }

    public async Task<DailyPlaylistDto?> PublishAsync(Guid id)
    {
        var playlist = await _playlistRepository.GetByIdAsync(id);
        if (playlist is null || playlist.Status == DailyPlaylistStatus.Published)
        {
            return null;
        }

        playlist.Status = DailyPlaylistStatus.Published;
        playlist.PublishedAt = DateTime.UtcNow;
        playlist.UpdatedAt = DateTime.UtcNow;

        var updated = await _playlistRepository.UpdateAsync(playlist);
        return updated is null ? null : Map(updated);
    }

    private async Task<List<CampaignCreativeEntry>> GetEligibleEntriesAsync(
        IReadOnlyList<Campaign> campaigns,
        Building building,
        Screen screen,
        DateOnly date)
    {
        var currentDateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var eligibleCampaigns = new List<CampaignCreatives>();

        foreach (var campaign in campaigns.OrderBy(item => item.Id))
        {
            var isEligible = await _eligibilityService.IsEligibleAsync(
                campaign.Id,
                building.City,
                building.BuildingType,
                screen.Orientation,
                currentDateTime);

            if (!isEligible)
            {
                continue;
            }

            var creatives = await GetApprovedAssignedCreativesAsync(campaign.Id);
            if (creatives.Count == 0)
            {
                continue;
            }

            eligibleCampaigns.Add(new CampaignCreatives(campaign.Id, creatives));
        }

        return BuildRoundRobinItems(eligibleCampaigns);
    }

    private async Task<List<Creative>> GetApprovedAssignedCreativesAsync(Guid campaignId)
    {
        var assignments = await _campaignCreativeRepository.GetByCampaignIdAsync(campaignId);
        var creatives = new List<Creative>();

        foreach (var assignment in assignments.OrderBy(item => item.CreativeId))
        {
            var creative = await _creativeRepository.GetByIdAsync(assignment.CreativeId);
            if (creative?.ApprovalStatus == ApprovalStatus.Approved)
            {
                creatives.Add(creative);
            }
        }

        return creatives
            .OrderBy(creative => creative.Id)
            .ToList();
    }

    private async Task<DailyPlaylist> UpsertPlaylistAsync(
        Guid screenId,
        DateOnly date,
        IReadOnlyList<CampaignCreativeEntry> entries)
    {
        var now = DateTime.UtcNow;
        var existing = await _playlistRepository.GetByScreenAndDateAsync(screenId, date);

        if (existing is null)
        {
            var playlist = new DailyPlaylist
            {
                Id = Guid.NewGuid(),
                ScreenId = screenId,
                Date = date,
                Version = 1,
                Status = DailyPlaylistStatus.Draft,
                GeneratedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                Items = CreatePlaylistItems(Guid.NewGuid(), entries, now)
            };

            foreach (var item in playlist.Items)
            {
                item.DailyPlaylistId = playlist.Id;
            }

            return await _playlistRepository.AddAsync(playlist);
        }

        existing.Version += 1;
        existing.Status = DailyPlaylistStatus.Draft;
        existing.PublishedAt = null;
        existing.GeneratedAt = now;
        existing.UpdatedAt = now;
        existing.Items = CreatePlaylistItems(existing.Id, entries, now);

        return (await _playlistRepository.UpdateAsync(existing))!;
    }

    private static List<DailyPlaylistItem> CreatePlaylistItems(
        Guid playlistId,
        IReadOnlyList<CampaignCreativeEntry> entries,
        DateTime createdAt) =>
        entries.Select((entry, index) => new DailyPlaylistItem
        {
            Id = Guid.NewGuid(),
            DailyPlaylistId = playlistId,
            CampaignId = entry.CampaignId,
            CreativeId = entry.CreativeId,
            Order = index,
            DurationSeconds = entry.DurationSeconds,
            CreatedAt = createdAt
        }).ToList();

    private static List<CampaignCreativeEntry> BuildRoundRobinItems(IReadOnlyList<CampaignCreatives> campaigns)
    {
        var items = new List<CampaignCreativeEntry>();
        var round = 0;

        while (true)
        {
            var addedInRound = false;

            foreach (var campaign in campaigns)
            {
                if (round >= campaign.Creatives.Count)
                {
                    continue;
                }

                var creative = campaign.Creatives[round];
                items.Add(new CampaignCreativeEntry(campaign.CampaignId, creative.Id, creative.DurationSeconds));
                addedInRound = true;
            }

            if (!addedInRound)
            {
                break;
            }

            round++;
        }

        return items;
    }

    private static DailyPlaylistDto Map(DailyPlaylist playlist) =>
        new(
            playlist.Id,
            playlist.ScreenId,
            playlist.Date,
            playlist.Version,
            playlist.Status,
            playlist.GeneratedAt,
            playlist.PublishedAt,
            playlist.CreatedAt,
            playlist.UpdatedAt,
            playlist.Items
                .OrderBy(item => item.Order)
                .Select(item => new DailyPlaylistItemDto(
                    item.Id,
                    item.DailyPlaylistId,
                    item.CampaignId,
                    item.CreativeId,
                    item.Order,
                    item.DurationSeconds,
                    item.CreatedAt))
                .ToList());

    private sealed record CampaignCreatives(Guid CampaignId, IReadOnlyList<Creative> Creatives);
    private sealed record CampaignCreativeEntry(Guid CampaignId, Guid CreativeId, int DurationSeconds);
}
