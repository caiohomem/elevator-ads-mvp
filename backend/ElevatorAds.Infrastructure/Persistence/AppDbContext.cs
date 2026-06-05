using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<Advertiser> Advertisers => Set<Advertiser>();
    public DbSet<Creative> Creatives => Set<Creative>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignBookingRequest> CampaignBookingRequests => Set<CampaignBookingRequest>();
    public DbSet<CampaignForecast> CampaignForecasts => Set<CampaignForecast>();
    public DbSet<CampaignCreative> CampaignCreatives => Set<CampaignCreative>();
    public DbSet<CampaignDeliveryConstraints> CampaignDeliveryConstraints => Set<CampaignDeliveryConstraints>();
    public DbSet<InventoryPackage> InventoryPackages => Set<InventoryPackage>();
    public DbSet<DailyPlaylist> DailyPlaylists => Set<DailyPlaylist>();
    public DbSet<DailyPlaylistItem> DailyPlaylistItems => Set<DailyPlaylistItem>();
    public DbSet<ProofOfPlayEvent> ProofOfPlayEvents => Set<ProofOfPlayEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
