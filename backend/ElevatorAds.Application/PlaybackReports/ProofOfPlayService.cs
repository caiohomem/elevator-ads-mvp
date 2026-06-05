using ElevatorAds.Domain.Common;
using ElevatorAds.Application.PlaybackReports.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.PlaybackReports;

public sealed class ProofOfPlayService
{
    private readonly IProofOfPlayEventRepository _proofOfPlayRepository;
    private readonly IScreenRepository _screenRepository;
    private readonly IDailyPlaylistRepository _playlistRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICreativeRepository _creativeRepository;

    public ProofOfPlayService(
        IProofOfPlayEventRepository proofOfPlayRepository,
        IScreenRepository screenRepository,
        IDailyPlaylistRepository playlistRepository,
        ICampaignRepository campaignRepository,
        ICreativeRepository creativeRepository)
    {
        _proofOfPlayRepository = proofOfPlayRepository;
        _screenRepository = screenRepository;
        _playlistRepository = playlistRepository;
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
    }

    public async Task<CreatePlaybackReportResult> CreateAsync(Guid screenId, CreatePlaybackReportRequest request)
    {
        var screen = await _screenRepository.GetByIdAsync(screenId);
        if (screen is null)
        {
            return CreatePlaybackReportResult.NotFound();
        }

        var playlist = await _playlistRepository.GetByIdAsync(request.PlaylistId);
        if (playlist is null || playlist.ScreenId != screenId)
        {
            return CreatePlaybackReportResult.NotFound();
        }

        var item = playlist.Items.FirstOrDefault(candidate => candidate.Id == request.PlaylistItemId);
        if (item is null)
        {
            return CreatePlaybackReportResult.NotFound();
        }

        if (!request.PlayedAt.HasValue)
        {
            return CreatePlaybackReportResult.Invalid("PlayedAt is required.");
        }

        if (request.DurationSeconds <= 0)
        {
            return CreatePlaybackReportResult.Invalid("DurationSeconds must be greater than 0.");
        }

        var now = DateTime.UtcNow;
        var proofOfPlay = new ProofOfPlayEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = screen.OrganizationId,
            ScreenId = screenId,
            PlaylistId = playlist.Id,
            PlaylistItemId = item.Id,
            CampaignId = item.CampaignId,
            CreativeId = item.CreativeId,
            PlayedAt = request.PlayedAt.Value,
            DurationSeconds = request.DurationSeconds,
            CreatedAt = now
        };

        var created = await _proofOfPlayRepository.AddAsync(proofOfPlay);
        var campaignName = (await _campaignRepository.GetByIdAsync(item.CampaignId))?.Name ?? item.CampaignId.ToString();
        var creativeName = (await _creativeRepository.GetByIdAsync(item.CreativeId))?.Name ?? item.CreativeId.ToString();
        return CreatePlaybackReportResult.Success(Map(
            created,
            new Dictionary<Guid, string> { [screen.Id] = string.IsNullOrWhiteSpace(screen.Name) ? screen.ExternalCode : screen.Name },
            new Dictionary<Guid, string> { [item.CampaignId] = campaignName },
            new Dictionary<Guid, string> { [item.CreativeId] = creativeName }));
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetAllAsync()
    {
        var events = (await _proofOfPlayRepository.GetAllAsync()).ToList();
        return await MapAsync(events);
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedAsync(query);
        var mappedItems = await MapAsync(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetByScreenAsync(Guid screenId)
    {
        var events = (await _proofOfPlayRepository.GetByScreenIdAsync(screenId)).ToList();
        return await MapAsync(events);
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedByScreenAsync(Guid screenId, PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedByScreenIdAsync(screenId, query);
        var mappedItems = await MapAsync(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetByCampaignAsync(Guid campaignId)
    {
        var events = (await _proofOfPlayRepository.GetByCampaignIdAsync(campaignId)).ToList();
        return await MapAsync(events);
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedByCampaignAsync(Guid campaignId, PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedByCampaignIdAsync(campaignId, query);
        var mappedItems = await MapAsync(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    private async Task<IReadOnlyList<PlaybackReportDto>> MapAsync(IEnumerable<ProofOfPlayEvent> events)
    {
        var items = events.ToList();
        if (items.Count == 0)
        {
            return [];
        }

        var screenNames = await BuildScreenNamesAsync(items.Select(item => item.ScreenId));
        var campaignNames = await BuildCampaignNamesAsync(items.Select(item => item.CampaignId));
        var creativeNames = await BuildCreativeNamesAsync(items.Select(item => item.CreativeId));

        return items.Select(item => Map(item, screenNames, campaignNames, creativeNames)).ToList();
    }

    private static PlaybackReportDto Map(
        ProofOfPlayEvent proofOfPlay,
        IReadOnlyDictionary<Guid, string> screenNames,
        IReadOnlyDictionary<Guid, string> campaignNames,
        IReadOnlyDictionary<Guid, string> creativeNames) =>
        new(
            proofOfPlay.Id,
            proofOfPlay.ScreenId,
            screenNames.GetValueOrDefault(proofOfPlay.ScreenId, proofOfPlay.ScreenId.ToString()),
            proofOfPlay.PlaylistId,
            proofOfPlay.PlaylistItemId,
            proofOfPlay.CampaignId,
            campaignNames.GetValueOrDefault(proofOfPlay.CampaignId, proofOfPlay.CampaignId.ToString()),
            proofOfPlay.CreativeId,
            creativeNames.GetValueOrDefault(proofOfPlay.CreativeId, proofOfPlay.CreativeId.ToString()),
            proofOfPlay.PlayedAt,
            proofOfPlay.DurationSeconds,
            proofOfPlay.CreatedAt);

    private async Task<IReadOnlyDictionary<Guid, string>> BuildScreenNamesAsync(IEnumerable<Guid> screenIds)
    {
        var ids = screenIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _screenRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => string.IsNullOrWhiteSpace(item.Name) ? item.ExternalCode : item.Name);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildCampaignNamesAsync(IEnumerable<Guid> campaignIds)
    {
        var ids = campaignIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _campaignRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => item.Name);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildCreativeNamesAsync(IEnumerable<Guid> creativeIds)
    {
        var ids = creativeIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _creativeRepository.GetAllAsync())
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id, item => item.Name);
    }

    public sealed record CreatePlaybackReportResult(bool IsSuccess, bool WasFound, string? Error, PlaybackReportDto? Value)
    {
        public static CreatePlaybackReportResult Success(PlaybackReportDto value) => new(true, true, null, value);

        public static CreatePlaybackReportResult NotFound() => new(false, false, null, null);

        public static CreatePlaybackReportResult Invalid(string error) => new(false, true, error, null);
    }
}
