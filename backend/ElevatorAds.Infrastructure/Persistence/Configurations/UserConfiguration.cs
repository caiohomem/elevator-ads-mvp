using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Username).IsRequired().HasMaxLength(100);
        builder.HasIndex(item => item.Username).IsUnique();

        builder.Property(item => item.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(item => item.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
    }
}
