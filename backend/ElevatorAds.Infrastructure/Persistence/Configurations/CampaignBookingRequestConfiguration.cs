using System.Text.Json;
using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CampaignBookingRequestConfiguration : IEntityTypeConfiguration<CampaignBookingRequest>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<CampaignBookingRequest> builder)
    {
        builder.ToTable("CampaignBookingRequests");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.AdvertiserId).IsRequired();
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
        builder.Property(item => item.DateFrom).IsRequired();
        builder.Property(item => item.DateTo).IsRequired();
        builder.Property(item => item.CreativeDurationSeconds).IsRequired();
        builder.Property(item => item.Budget).HasColumnType("numeric(18,4)");
        builder.Property(item => item.CampaignObjective).IsRequired().HasMaxLength(500);
        builder.Property(item => item.Notes).IsRequired().HasMaxLength(4000);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.Property(item => item.Cities)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListComparer());

        builder.Property(item => item.BuildingTypes)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListComparer());

        builder.Property(item => item.ScreenOrientations)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListComparer());
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
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
