using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class AdvertiserConfiguration : IEntityTypeConfiguration<Advertiser>
{
    public void Configure(EntityTypeBuilder<Advertiser> builder)
    {
        builder.ToTable("Advertisers");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.LegalName).IsRequired().HasMaxLength(300);
        builder.Property(item => item.TaxId).IsRequired().HasMaxLength(100);
        builder.Property(item => item.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(item => item.ContactEmail).IsRequired().HasMaxLength(300);
        builder.Property(item => item.Phone).IsRequired().HasMaxLength(50);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
    }
}
