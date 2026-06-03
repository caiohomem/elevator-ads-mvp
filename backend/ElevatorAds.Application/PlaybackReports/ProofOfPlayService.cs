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

    public ProofOfPlayService(
        IProofOfPlayEventRepository proofOfPlayRepository,
        IScreenRepository screenRepository,
        IDailyPlaylistRepository playlistRepository)
    {
        _proofOfPlayRepository = proofOfPlayRepository;
        _screenRepository = screenRepository;
        _playlistRepository = playlistRepository;
    }

    public async Task<CreatePlaybackReportResult> CreateAsync(Guid screenId, CreatePlaybackReportRequest request)
    {
        if (await _screenRepository.GetByIdAsync(screenId) is null)
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
        return CreatePlaybackReportResult.Success(Map(created));
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetAllAsync()
    {
        var events = await _proofOfPlayRepository.GetAllAsync();
        return events.Select(Map).ToList();
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetByScreenAsync(Guid screenId)
    {
        var events = await _proofOfPlayRepository.GetByScreenIdAsync(screenId);
        return events.Select(Map).ToList();
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedByScreenAsync(Guid screenId, PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedByScreenIdAsync(screenId, query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<IReadOnlyList<PlaybackReportDto>> GetByCampaignAsync(Guid campaignId)
    {
        var events = await _proofOfPlayRepository.GetByCampaignIdAsync(campaignId);
        return events.Select(Map).ToList();
    }

    public async Task<PagedResult<PlaybackReportDto>> GetPagedByCampaignAsync(Guid campaignId, PagedQuery query)
    {
        var (items, totalCount) = await _proofOfPlayRepository.GetPagedByCampaignIdAsync(campaignId, query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<PlaybackReportDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    private static PlaybackReportDto Map(ProofOfPlayEvent proofOfPlay) =>
        new(
            proofOfPlay.Id,
            proofOfPlay.ScreenId,
            proofOfPlay.PlaylistId,
            proofOfPlay.PlaylistItemId,
            proofOfPlay.CampaignId,
            proofOfPlay.CreativeId,
            proofOfPlay.PlayedAt,
            proofOfPlay.DurationSeconds,
            proofOfPlay.CreatedAt);

    public sealed record CreatePlaybackReportResult(bool IsSuccess, bool WasFound, string? Error, PlaybackReportDto? Value)
    {
        public static CreatePlaybackReportResult Success(PlaybackReportDto value) => new(true, true, null, value);

        public static CreatePlaybackReportResult NotFound() => new(false, false, null, null);

        public static CreatePlaybackReportResult Invalid(string error) => new(false, true, error, null);
    }
}
