using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class ScreenConfiguration : IEntityTypeConfiguration<Screen>
{
    public void Configure(EntityTypeBuilder<Screen> builder)
    {
        builder.ToTable("Screens");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.BuildingId).IsRequired();
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.ExternalCode).IsRequired().HasMaxLength(100);
        builder.Property(item => item.ResolutionWidth).IsRequired();
        builder.Property(item => item.ResolutionHeight).IsRequired();
        builder.Property(item => item.Orientation).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.LastSeenAt);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
    }
}
