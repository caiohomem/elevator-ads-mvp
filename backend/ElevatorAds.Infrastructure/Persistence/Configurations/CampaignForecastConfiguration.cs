using System.Text.Json;
using ElevatorAds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CampaignForecastConfiguration : IEntityTypeConfiguration<CampaignForecast>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<CampaignForecast> builder)
    {
        builder.ToTable("CampaignForecasts");
        builder.HasKey(item => item.Id);

        builder.HasIndex(item => item.BookingRequestId).IsUnique();

        builder.Property(item => item.BookingRequestId).IsRequired();
        builder.Property(item => item.EligibleScreens).IsRequired();
        builder.Property(item => item.EligibleBuildings).IsRequired();
        builder.Property(item => item.EstimatedPlays).IsRequired();
        builder.Property(item => item.EstimatedAudience).IsRequired();
        builder.Property(item => item.EstimatedCost).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(item => item.AvailableCapacity).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.Property(item => item.Warnings)
            .HasColumnType("jsonb")
            .HasConversion(CreateListStringConverter())
            .Metadata.SetValueComparer(CreateListComparer());

        builder.Property(item => item.Conflicts)
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
