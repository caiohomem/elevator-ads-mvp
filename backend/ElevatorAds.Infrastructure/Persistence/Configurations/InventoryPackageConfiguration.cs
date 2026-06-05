using System.Text.Json;
using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class InventoryPackageConfiguration : IEntityTypeConfiguration<InventoryPackage>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<InventoryPackage> builder)
    {
        builder.ToTable("InventoryPackages");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.BaseCpm).HasColumnType("numeric(18,4)");
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.Property(item => item.Cities)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListStringComparer());

        builder.Property(item => item.BuildingTypes)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListStringComparer());

        builder.Property(item => item.ScreenOrientations)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListStringComparer());

        builder.Property(item => item.ScreenIds)
            .HasColumnType("jsonb")
            .HasConversion(CreateListGuidConverter())
            .Metadata.SetValueComparer(CreateListGuidComparer());

        builder.Property(item => item.BuildingIds)
            .HasColumnType("jsonb")
            .HasConversion(CreateListGuidConverter())
            .Metadata.SetValueComparer(CreateListGuidComparer());
    }

    private static ValueConverter<List<string>, string> CreateListStringConverter() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeStringList(value) ?? new List<string>());

    private static ValueComparer<List<string>> CreateListStringComparer() =>
        new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToList());

    private static ValueConverter<List<Guid>, string> CreateListGuidConverter() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeGuidList(value) ?? new List<Guid>());

    private static ValueComparer<List<Guid>> CreateListGuidComparer() =>
        new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToList());

    private static List<string>? DeserializeStringList(string json)
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

    private static List<Guid>? DeserializeGuidList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
