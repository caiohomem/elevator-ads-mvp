using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class DailyPlaylistConfiguration : IEntityTypeConfiguration<DailyPlaylist>
{
    public void Configure(EntityTypeBuilder<DailyPlaylist> builder)
    {
        builder.ToTable("DailyPlaylists");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ScreenId).IsRequired();
        builder.Property(item => item.Date).IsRequired().HasColumnType("date");
        builder.Property(item => item.Version).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.GeneratedAt).IsRequired();
        builder.Property(item => item.PublishedAt);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.HasMany(item => item.Items)
            .WithOne()
            .HasForeignKey(item => item.DailyPlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.ScreenId, item.Date, item.Version }).IsUnique();

        builder.Property(item => item.OrganizationId).IsRequired();
        builder.HasOne(item => item.Organization)
            .WithMany()
            .HasForeignKey(item => item.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
