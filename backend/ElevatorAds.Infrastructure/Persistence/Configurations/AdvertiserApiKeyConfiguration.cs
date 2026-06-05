using System.Text.Json;
using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class AdvertiserApiKeyConfiguration : IEntityTypeConfiguration<AdvertiserApiKey>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<AdvertiserApiKey> builder)
    {
        builder.ToTable("AdvertiserApiKeys");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.AdvertiserId).IsRequired();
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.KeyPrefix).IsRequired().HasMaxLength(32);
        builder.Property(item => item.KeyHash).IsRequired().HasMaxLength(64);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.ExpiresAt);
        builder.Property(item => item.LastUsedAt);
        builder.Property(item => item.RevokedAt);

        builder.Property(item => item.Scopes)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListComparer());

        builder.HasIndex(item => item.AdvertiserId);
        builder.HasIndex(item => item.KeyPrefix);

        builder.HasOne(item => item.Advertiser)
            .WithMany()
            .HasForeignKey(item => item.AdvertiserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static ValueConverter<List<string>, string> CreateListStringConverter() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeList(value) ?? new List<string>());

    private static ValueComparer<List<string>> CreateListComparer() =>
        new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToList());

    private static List<string>? DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
