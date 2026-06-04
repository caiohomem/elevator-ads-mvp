using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.AdvertiserId).IsRequired();
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.StartDate);
        builder.Property(item => item.EndDate);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.DailyBudget).HasColumnType("numeric(18,4)");
        builder.Property(item => item.TotalBudget).HasColumnType("numeric(18,4)");
        builder.Property(item => item.MaxCpm).HasColumnType("numeric(18,4)");
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.Property(item => item.OrganizationId).IsRequired();
        builder.HasOne(item => item.Organization)
            .WithMany()
            .HasForeignKey(item => item.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
