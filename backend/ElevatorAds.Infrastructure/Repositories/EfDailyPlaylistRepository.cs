using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfDailyPlaylistRepository : IDailyPlaylistRepository
{
    private readonly Persistence.AppDbContext _context;

    public EfDailyPlaylistRepository(Persistence.AppDbContext context) => _context = context;

    public async Task<IEnumerable<DailyPlaylist>> GetAllAsync() =>
        await _context.DailyPlaylists
            .Include(item => item.Items)
            .AsNoTracking()
            .OrderBy(item => item.Date)
            .ThenBy(item => item.ScreenId)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync();

    public async Task<DailyPlaylist?> GetByIdAsync(Guid id) =>
        await _context.DailyPlaylists
            .Include(item => item.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

    public async Task<DailyPlaylist?> GetByScreenAndDateAsync(Guid screenId, DateOnly date) =>
        await _context.DailyPlaylists
            .Include(item => item.Items)
            .AsNoTracking()
            .Where(item => item.ScreenId == screenId && item.Date == date)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync();

    public async Task<DailyPlaylist?> GetLatestPublishedByScreenAndDateAsync(Guid screenId, DateOnly date) =>
        await _context.DailyPlaylists
            .Include(item => item.Items)
            .AsNoTracking()
            .Where(item => item.ScreenId == screenId && item.Date == date && item.Status == DailyPlaylistStatus.Published)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<DailyPlaylist>> GetByScreenIdAsync(Guid screenId, DateOnly? date) =>
        await _context.DailyPlaylists
            .Include(item => item.Items)
            .AsNoTracking()
            .Where(item => item.ScreenId == screenId && (date == null || item.Date == date.Value))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Version)
            .ToListAsync();

    public async Task<DailyPlaylist> AddAsync(DailyPlaylist playlist)
    {
        await _context.DailyPlaylists.AddAsync(playlist);
        await _context.SaveChangesAsync();
        return playlist;
    }

    public async Task<DailyPlaylist?> UpdateAsync(DailyPlaylist playlist)
    {
        var existing = await _context.DailyPlaylists
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == playlist.Id);
        if (existing is null)
        {
            return null;
        }

        playlist.UpdatedAt = DateTime.UtcNow;
        _context.Entry(existing).CurrentValues.SetValues(playlist);

        if (existing.Items is not null)
        {
            _context.DailyPlaylistItems.RemoveRange(existing.Items);
        }

        if (playlist.Items is { Count: > 0 })
        {
            await _context.DailyPlaylistItems.AddRangeAsync(playlist.Items);
        }

        await _context.SaveChangesAsync();
        return existing;
    }
}
