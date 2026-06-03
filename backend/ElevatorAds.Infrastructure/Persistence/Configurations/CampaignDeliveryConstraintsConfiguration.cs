using System.Text.Json;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElevatorAds.Infrastructure.Persistence.Configurations;

public sealed class CampaignDeliveryConstraintsConfiguration : IEntityTypeConfiguration<CampaignDeliveryConstraints>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<CampaignDeliveryConstraints> builder)
    {
        builder.ToTable("CampaignDeliveryConstraints");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.CampaignId).IsRequired();

        var listStringConverter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeList<string>(value) ?? new List<string>());

        var listBuildingTypeConverter = new ValueConverter<List<BuildingType>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeEnumList<BuildingType>(value));

        var listScreenOrientationConverter = new ValueConverter<List<ScreenOrientation>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeEnumList<ScreenOrientation>(value));

        var listDayOfWeekConverter = new ValueConverter<List<DayOfWeek>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeEnumList<DayOfWeek>(value));

        builder.Property(item => item.Cities)
            .HasColumnType("jsonb")
            .HasConversion(listStringConverter)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));

        builder.Property(item => item.BuildingTypes)
            .HasColumnType("jsonb")
            .HasConversion(listBuildingTypeConverter)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<BuildingType>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));

        builder.Property(item => item.ScreenOrientations)
            .HasColumnType("jsonb")
            .HasConversion(listScreenOrientationConverter)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<ScreenOrientation>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));

        builder.Property(item => item.DaysOfWeek)
            .HasColumnType("jsonb")
            .HasConversion(listDayOfWeekConverter)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<DayOfWeek>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));

        builder.Property(item => item.StartTime);
        builder.Property(item => item.EndTime);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.HasIndex(item => item.CampaignId).IsUnique();
    }

    private static List<T>? DeserializeList<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch (JsonException)
        {
            return new List<T>();
        }
    }

    private static List<TEnum> DeserializeEnumList<TEnum>(string json) where TEnum : struct, Enum
    {
        var values = DeserializeList<int>(json) ?? new List<int>();
        var result = new List<TEnum>();
        foreach (var value in values)
        {
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                result.Add((TEnum)Enum.ToObject(typeof(TEnum), value));
            }
        }

        return result;
    }
}
