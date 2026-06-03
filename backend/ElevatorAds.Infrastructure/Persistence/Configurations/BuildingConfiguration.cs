using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.Address).IsRequired().HasMaxLength(500);
        builder.Property(item => item.City).IsRequired().HasMaxLength(200);
        builder.Property(item => item.Country).IsRequired().HasMaxLength(200);
        builder.Property(item => item.PostalCode).IsRequired().HasMaxLength(50);
        builder.Property(item => item.BuildingType).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.EstimatedDailyAudience).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
    }
}
