using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CampaignCreativeConfiguration : IEntityTypeConfiguration<CampaignCreative>
{
    public void Configure(EntityTypeBuilder<CampaignCreative> builder)
    {
        builder.ToTable("CampaignCreatives");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.CampaignId).IsRequired();
        builder.Property(item => item.CreativeId).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();

        builder.HasIndex(item => new { item.CampaignId, item.CreativeId }).IsUnique();
    }
}
