using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class ProofOfPlayEventConfiguration : IEntityTypeConfiguration<ProofOfPlayEvent>
{
    public void Configure(EntityTypeBuilder<ProofOfPlayEvent> builder)
    {
        builder.ToTable("ProofOfPlayEvents");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ScreenId).IsRequired();
        builder.Property(item => item.PlaylistId).IsRequired();
        builder.Property(item => item.PlaylistItemId).IsRequired();
        builder.Property(item => item.CampaignId).IsRequired();
        builder.Property(item => item.CreativeId).IsRequired();
        builder.Property(item => item.PlayedAt).IsRequired();
        builder.Property(item => item.DurationSeconds).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
    }
}
