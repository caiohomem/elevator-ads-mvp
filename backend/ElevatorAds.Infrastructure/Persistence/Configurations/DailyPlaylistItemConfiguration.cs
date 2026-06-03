using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class DailyPlaylistItemConfiguration : IEntityTypeConfiguration<DailyPlaylistItem>
{
    public void Configure(EntityTypeBuilder<DailyPlaylistItem> builder)
    {
        builder.ToTable("DailyPlaylistItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.DailyPlaylistId).IsRequired();
        builder.Property(item => item.CampaignId).IsRequired();
        builder.Property(item => item.CreativeId).IsRequired();
        builder.Property(item => item.Order).IsRequired();
        builder.Property(item => item.DurationSeconds).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();

        builder.HasIndex(item => new { item.DailyPlaylistId, item.Order }).IsUnique();
    }
}
