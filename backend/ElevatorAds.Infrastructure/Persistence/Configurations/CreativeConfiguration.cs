using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CreativeConfiguration : IEntityTypeConfiguration<Creative>
{
    public void Configure(EntityTypeBuilder<Creative> builder)
    {
        builder.ToTable("Creatives");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.AdvertiserId).IsRequired();
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.MediaUrl).IsRequired().HasMaxLength(1000);
        builder.Property(item => item.MediaType).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.DurationSeconds).IsRequired();
        builder.Property(item => item.ApprovalStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
    }
}
