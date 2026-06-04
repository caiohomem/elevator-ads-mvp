using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(item => item.Slug).IsUnique();

        builder.Property(item => item.Status).IsRequired().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
    }
}
